using UnityEngine;
using System.Collections;
using FSM;

namespace FSM
{
    public class BattleStateInit : IBattleState
    {
        private TurnManager tm;

        public BattleStateInit(TurnManager turnManager)
        {
            tm = turnManager;
        }

        public void Enter()
        {
            Debug.Log("[FSM] Entering InitState");
            tm.ResolveReferences();

            tm.SetBattleEnded(false);
            tm.SetIsBattleActive(true);
            tm.SetIsPlayerTurn(false);

            if (tm.playerStats != null) tm.playerStats.InitializeStats();
            if (tm.enemyStats != null)
            {
                tm.enemyStats.InitializeStats();
                
                // Ensure the monster has the animation bridge and plays idle
                var animBridge = tm.enemyStats.GetComponent<MonsterAnimationBridge>();
                if (animBridge == null)
                    animBridge = tm.enemyStats.gameObject.AddComponent<MonsterAnimationBridge>();
                animBridge.PlayIdle();
            }

            if (tm.enemyIntent != null)
            {
                tm.enemyIntent.stats = tm.enemyStats;
                tm.enemyIntent.RefreshWeaknessList();
                tm.enemyIntent.ResetIntent();
                tm.SetCurrentEnemyIntent(tm.enemyIntent.CurrentDisplayName);
            }

            if (tm.cardDeck != null)
            {
                tm.cardDeck.ClearAllForBattleReset();
                tm.cardDeck.InitializeDeck();
                tm.cardDeck.DrawInitialHand(tm.initialDrawAmount);
            }

            tm.SetCurrentTurnIndex(1);

            tm.InvokeBattleStarted();
            tm.InvokeTurnInfoChanged();

            tm.StateMachine.TransitionTo(new BattleStatePlayerTurn(tm, false));
        }

        public void Update() {}
        public void Exit() {}
    }

    public class BattleStatePlayerTurn : IBattleState
    {
        private TurnManager tm;
        private bool drawCards;

        public BattleStatePlayerTurn(TurnManager turnManager, bool drawCards)
        {
            tm = turnManager;
            this.drawCards = drawCards;
        }

        public void Enter()
        {
            Debug.Log($"[FSM] Entering PlayerTurnState (Turn {tm.CurrentTurnIndex})");
            if (tm.BattleEnded || !tm.IsBattleActive) return;

            tm.SetIsPlayerTurn(true);

            if (tm.playerStats != null)
                tm.playerStats.ResetEnergy();

            if (tm.enemyIntent != null)
            {
                tm.enemyIntent.PresentIntentForPlayerTurn();
                tm.SetCurrentEnemyIntent(tm.enemyIntent.CurrentDisplayName);
            }
            else
            {
                tm.SetCurrentEnemyIntent(tm.defaultEnemyIntent);
            }

            if (drawCards && tm.cardDeck != null)
            {
                tm.StartCoroutine(tm.cardDeck.DrawCardsOneByOneCoroutine(tm.drawPerTurn, 0.4f));
            }

            // Ensure monster is in Idle at the beginning of player turn
            if (tm.enemyStats != null)
            {
                var animBridge = tm.enemyStats.GetComponent<MonsterAnimationBridge>();
                if (animBridge != null) animBridge.PlayIdle();
            }

            tm.InvokePlayerTurnStarted();
            tm.InvokeTurnInfoChanged();
        }

        public void Update() {}
        public void Exit() {}
    }

    public class BattleStateEnemyTurn : IBattleState
    {
        private TurnManager tm;
        private Coroutine coroutine;

        public BattleStateEnemyTurn(TurnManager turnManager)
        {
            tm = turnManager;
        }

        public void Enter()
        {
            Debug.Log("[FSM] Entering EnemyTurnState");
            if (tm.BattleEnded || !tm.IsBattleActive) return;

            tm.SetIsPlayerTurn(false);
            tm.InvokeEnemyTurnStarted();
            tm.InvokeTurnInfoChanged();

            coroutine = tm.StartCoroutine(EnemyActionFlow());
        }

        private System.Collections.IEnumerator EnemyActionFlow()
        {
            // 1. Wait a moment for turn transition (e.g. 0.6 seconds)
            yield return new WaitForSeconds(0.6f);

            if (tm.BattleEnded || !tm.IsBattleActive) yield break;

            if (tm.enemyStats != null && tm.enemyStats.CurrentHP > 0)
            {
                var animBridge = tm.enemyStats.GetComponent<MonsterAnimationBridge>();
                if (animBridge == null)
                    animBridge = tm.enemyStats.gameObject.AddComponent<MonsterAnimationBridge>();

                // 2. Play attack/heavy animation depending on intent kind
                // Charge = 蓄势（不造成伤害）；Heavy = 释放重击；Attack = 普通/无弱点攻击
                if (animBridge != null && tm.enemyIntent != null && tm.enemyIntent.CurrentStep != null)
                {
                    var kind = tm.enemyIntent.CurrentStep.kind;
                    if (kind == EnemyIntentKind.Heavy)
                    {
                        animBridge.PlayHeavyAttack();
                        Debug.Log($"[FSM] PlayHeavyAttack: {tm.enemyIntent.CurrentStep.displayName}");
                    }
                    else if (kind == EnemyIntentKind.Attack)
                    {
                        // 高伤普攻（如石灵重击）也可用重击动画
                        bool hardHit = tm.enemyIntent.CurrentStep.power >= 10;
                        if (hardHit) animBridge.PlayHeavyAttack();
                        else animBridge.PlayAttack();
                        Debug.Log($"[FSM] PlayAttack(hard={hardHit}): {tm.enemyIntent.CurrentStep.displayName}");
                    }
                    else
                    {
                        // Defend / Charge 蓄势：保持 Idle 或基础动作
                        animBridge.PlayIdle();
                    }
                }

                // 3. Wait for the animation swing to connect (0.6s) before executing stats impact
                yield return new WaitForSeconds(0.6f);

                if (tm.BattleEnded || !tm.IsBattleActive) yield break;

                // 4. Apply damage and stats modifications
                if (tm.enemyIntent != null)
                {
                    tm.enemyIntent.ExecuteAndAdvance(tm.playerStats);
                }
                else if (tm.playerStats != null)
                {
                    tm.playerStats.TakeDamage(tm.enemyAttackDamage);
                    tm.enemyStats.ClearArmor();
                }
            }

            // 5. Check if player died
            if (tm.CheckBattleEnd()) yield break;

            // 敌方行动完成后结算灼烧；若灼烧击杀则直接转入战斗结束状态。
            if (tm.enemyStats != null)
                tm.enemyStats.ResolveBurnAtTurnEnd();

            if (tm.CheckBattleEnd()) yield break;

            // 6. Wait a bit after damage resolution (0.6s) then go back to player turn
            yield return new WaitForSeconds(0.6f);

            if (tm.BattleEnded || !tm.IsBattleActive) yield break;

            tm.SetCurrentTurnIndex(tm.CurrentTurnIndex + 1);
            tm.StateMachine.TransitionTo(new BattleStatePlayerTurn(tm, true));
        }

        public void Update() {}
        public void Exit()
        {
            if (coroutine != null && tm != null)
            {
                tm.StopCoroutine(coroutine);
            }
        }
    }

    public class BattleStateEnd : IBattleState
    {
        private TurnManager tm;
        private bool playerWon;

        public BattleStateEnd(TurnManager turnManager, bool playerWon)
        {
            tm = turnManager;
            this.playerWon = playerWon;
        }

        public void Enter()
        {
            Debug.Log($"[FSM] Entering BattleEndState. PlayerWon: {playerWon}");
            tm.SetBattleEnded(true);
            tm.SetIsBattleActive(false);
            tm.SetIsPlayerTurn(false);
            tm.StopAllCoroutines();

            tm.InvokeBattleEnded(playerWon);
            tm.InvokeTurnInfoChanged();
        }

        public void Update() {}
        public void Exit() {}
    }
}
