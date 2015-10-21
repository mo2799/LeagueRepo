﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Jinx_Genesis
{
    class Program
    {
        private static string ChampionName = "Jinx";

        public static Orbwalking.Orbwalker Orbwalker;
        public static Menu Config;

        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        private static Spell Q, W, E, R;
        private static float QMANA, WMANA, EMANA ,RMANA;
        private static bool FishBoneActive= false, Combo = false, Farm = false;

        private static List<Obj_AI_Hero> Enemies = new List<Obj_AI_Hero>();

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != ChampionName) return;

            LoadMenu();

            Q = new Spell(SpellSlot.Q, Player.AttackRange);
            W = new Spell(SpellSlot.W, 1490f);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 2500f);

            W.SetSkillshot(0.6f, 75f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.2f, 1f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.7f, 140f, 1500f, false, SkillshotType.SkillshotLine);

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsEnemy)
                {
                    Enemies.Add(hero);
                }
            }

            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.PrintChat("<font color=\"#00BFFF\">GENESIS </font>Jinx<font color=\"#000000\"> by Sebby </font> - <font color=\"#FFFFFF\">Loaded</font>");
        }

        private static void LoadMenu()
        {
            Config = new Menu(ChampionName + " GENESIS", ChampionName + " GENESIS", true);
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Config.AddToMainMenu();

            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("wRange", "W range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("eRange", "E range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("rRange", "R range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells").SetValue(true));

            Config.SubMenu("Q Config").AddItem(new MenuItem("Qcombo", "Combo Q").SetValue(true));
            Config.SubMenu("Q Config").AddItem(new MenuItem("Qharass", "Harass Q").SetValue(true));
            Config.SubMenu("Q Config").AddItem(new MenuItem("farmQout", "Farm Q out range AA minion").SetValue(true));
            Config.SubMenu("Q Config").AddItem(new MenuItem("Qchange", "Q change mode FishBone -> MiniGun").SetValue(new StringList(new[] { "Real Time", "Before AA"}, 1)));
            Config.SubMenu("Q Config").AddItem(new MenuItem("Qaoe", "Force FishBone if can hit x target").SetValue(new Slider(3, 5, 0)));
            Config.SubMenu("Q Config").AddItem(new MenuItem("QmanaIgnore", "Ignore mana if can kill in x AA").SetValue(new Slider(2, 10, 0)));

            Config.SubMenu("W Config").AddItem(new MenuItem("Wcombo", "Combo W").SetValue(true));
            Config.SubMenu("W Config").AddItem(new MenuItem("Wharass", "W harass").SetValue(true));
            Config.SubMenu("W Config").AddItem(new MenuItem("Wks", "W KS").SetValue(true));
            Config.SubMenu("W Config").AddItem(new MenuItem("Wts", "Harass mode").SetValue(new StringList(new[] { "Target selector", "All in range" }, 0)));
            Config.SubMenu("W Config").AddItem(new MenuItem("Wmode", "W mode").SetValue(new StringList(new[] { "Out range MiniGun", "Out range FishBone", "Custome range" }, 0)));
            Config.SubMenu("W Config").AddItem(new MenuItem("Wcustome", "Custome minimum range").SetValue(new Slider(600, 1500, 0)));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu("W Config").SubMenu("Harass enemy:").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu("E Config").AddItem(new MenuItem("Ecombo", "Combo E").SetValue(true));
            Config.SubMenu("E Config").AddItem(new MenuItem("Etel", "E on enemy teleport").SetValue(true));
            Config.SubMenu("E Config").AddItem(new MenuItem("Ecc", "E on CC").SetValue(true));
            Config.SubMenu("E Config").AddItem(new MenuItem("Eslow", "E on slow").SetValue(true));
            Config.SubMenu("E Config").AddItem(new MenuItem("Edash", "E on dash").SetValue(true));
            Config.SubMenu("E Config").SubMenu("E Gap Closer").AddItem(new MenuItem("EmodeGC", "Gap Closer position mode").SetValue(new StringList(new[] { "Dash end position", "Jinx position"}, 0)));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu("E Config").SubMenu("E Gap Closer").SubMenu("Cast on enemy:").AddItem(new MenuItem("EGCchampion" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu("R Config").AddItem(new MenuItem("Rks", "R KS").SetValue(true));
            Config.SubMenu("R Config").SubMenu("Semi-manual cast R").AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space
            Config.SubMenu("R Config").SubMenu("Semi-manual cast R").AddItem(new MenuItem("semiMode", "Semi-manual cast mode").SetValue(new StringList(new[] { "Low hp target", "AOE"}, 0)));
            Config.SubMenu("R Config").AddItem(new MenuItem("Rmode", "R mode").SetValue(new StringList(new[] { "Out range MiniGun ", "Out range FishBone ", "Custome range " }, 0)));
            Config.SubMenu("R Config").AddItem(new MenuItem("Rcustome", "Custome minimum range").SetValue(new Slider(1000, 1600, 0)));
            Config.SubMenu("R Config").AddItem(new MenuItem("RcustomeMax", "Max range").SetValue(new Slider(3000, 10000, 0)));
            Config.SubMenu("R Config").AddItem(new MenuItem("Raoe", "R if can hit x target and can kill").SetValue(new Slider(2, 5, 0)));
            Config.SubMenu("R Config").SubMenu("OverKill protrection").AddItem(new MenuItem("Rover", "Don't R if allies near target in x range ").SetValue(new Slider(500, 1000, 0)));
            Config.SubMenu("R Config").SubMenu("OverKill protrection").AddItem(new MenuItem("RoverAA", "Don't R if Jinx winding up").SetValue(true));
            Config.SubMenu("R Config").SubMenu("OverKill protrection").AddItem(new MenuItem("RoverW", "Don't R if can W KS").SetValue(true));

            //Config.SubMenu("MISC").SubMenu("Use harass mode").AddItem(new MenuItem("LaneClearmode", "LaneClear").SetValue(true));
            //Config.SubMenu("MISC").SubMenu("Use harass mode").AddItem(new MenuItem("Mixedmode", "Mixed").SetValue(true));
            //Config.SubMenu("MISC").SubMenu("Use harass mode").AddItem(new MenuItem("LastHitmode", "LastHit").SetValue(true));

            //Config.SubMenu("Mana Manager").AddItem(new MenuItem("ManaKs", "always safe mana to KS R or W").SetValue(true));
            Config.SubMenu("Mana Manager").AddItem(new MenuItem("QmanaCombo", "Q combo mana").SetValue(new Slider(20, 100, 0)));
            Config.SubMenu("Mana Manager").AddItem(new MenuItem("QmanaHarass", "Q harass mana").SetValue(new Slider(40, 100, 0)));
            Config.SubMenu("Mana Manager").AddItem(new MenuItem("WmanaCombo", "W combo mana").SetValue(new Slider(20, 100, 0)));
            Config.SubMenu("Mana Manager").AddItem(new MenuItem("WmanaHarass", "W harass mana").SetValue(new Slider(40, 100, 0)));
            Config.SubMenu("Mana Manager").AddItem(new MenuItem("EmanaCombo", "E mana").SetValue(new Slider(20, 100, 0)));

            //Config.Item("Qchange").GetValue<StringList>().SelectedIndex == 1
            //Config.Item("haras" + enemy.ChampionName).GetValue<bool>()
            //Config.Item("QmanaCombo").GetValue<Slider>().Value
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.ManaPercent < Config.Item("EmanaCombo").GetValue<Slider>().Value)
                return;

            if (E.IsReady())
            {
                var t = gapcloser.Sender;
                if (t.IsValidTarget(E.Range) && Config.Item("EGCchampion" + t.ChampionName).GetValue<bool>())
                {
                    if(Config.Item("EmodeGC").GetValue<StringList>().SelectedIndex == 0)
                        E.Cast(gapcloser.End);
                    else
                        E.Cast(Player.ServerPosition);
                }
            }
        }

        private static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Q.IsReady() && FishBoneActive && args.Target is Obj_AI_Hero && Config.Item("Qchange").GetValue<StringList>().SelectedIndex == 1)
            {
                Console.WriteLine(args.Target.Name);
                var t = (Obj_AI_Hero)args.Target;
                if ( t.IsValidTarget())
                {
                    FishBoneToMiniGun(t);
                }
            }

            if (Farm && FishBoneActive && args.Target is Obj_AI_Minion)
            {
                var t = (Obj_AI_Minion)args.Target;
                if (GetRealDistance(t) < GetRealPowPowRange(t))
                {
                    args.Process = false;
                    if (Q.IsReady())
                        Q.Cast();
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            SetValues();
            if (Config.Item("Wmode").GetValue<StringList>().SelectedIndex == 2)
                Config.Item("Wcustome").Show(true);
            else
                Config.Item("Wcustome").Show(false);

            if (Config.Item("Rmode").GetValue<StringList>().SelectedIndex == 2)
                Config.Item("Rcustome").Show(true);
            else
                Config.Item("Rcustome").Show(false);

            if (Q.IsReady())
                Qlogic();
            if (W.IsReady())
                Wlogic();
            if (E.IsReady())
                Elogic();
            if (R.IsReady())
                Rlogic();
        }

        private static void Rlogic()
        {
            R.Range = Config.Item("RcustomeMax").GetValue<Slider>().Value;

            if (Config.Item("useR").GetValue<KeyBind>().Active)
            {
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if(Config.Item("semiMode").GetValue<StringList>().SelectedIndex == 0)
                    {
                        R.Cast(t);
                    }
                    else
                    {
                        R.CastIfWillHit(t, 2);
                        R.Cast(t, true, true);
                    }
                }   
            }

            if (Config.Item("Rks").GetValue<bool>())
            {
                bool cast = false;
                

                if (Config.Item("RoverAA").GetValue<bool>() && (!Orbwalking.CanAttack() || Player.IsWindingUp))
                    return;

                foreach (var target in Enemies.Where(target => target.IsValidTarget(R.Range) && ValidUlt(target) ))
                {
                    
                    float predictedHealth = target.Health + target.HPRegenRate * 2;
                    var Rdmg = R.GetDamage(target, 1);

                    if (Rdmg > predictedHealth)
                    {
                        cast = true;
                        PredictionOutput output = R.GetPrediction(target);
                        Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
                        direction.Normalize();

                        foreach (var enemy in Enemies.Where(enemy => enemy.IsValidTarget()))
                        {
                            if (enemy.SkinName == target.SkinName || !cast)
                                continue;
                            PredictionOutput prediction = R.GetPrediction(enemy);
                            Vector3 predictedPosition = prediction.CastPosition;
                            Vector3 v = output.CastPosition - Player.ServerPosition;
                            Vector3 w = predictedPosition - Player.ServerPosition;
                            double c1 = Vector3.Dot(w, v);
                            double c2 = Vector3.Dot(v, v);
                            double b = c1 / c2;
                            Vector3 pb = Player.ServerPosition + ((float)b * v);
                            float length = Vector3.Distance(predictedPosition, pb);
                            if (length < (R.Width + 150 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                cast = false;
                        }

                        if (cast)
                        {

                            if (target.CountEnemiesInRange(400) > Config.Item("Raoe").GetValue<Slider>().Value)
                                R.CastIfHitchanceEquals(target, HitChance.VeryHigh);
                            if (Config.Item("RoverW").GetValue<bool>() && target.IsValidTarget(W.Range-200) && W.GetDamage(target) > target.Health &&  (W.Instance.CooldownExpires - Game.Time <  2 || W.Instance.CooldownExpires - Game.Time + 3> W.Instance.Cooldown))
                                return;
                            if (WValidRange(target) && target.CountAlliesInRange(Config.Item("Rover").GetValue<Slider>().Value) == 0)
                                R.CastIfHitchanceEquals(target, HitChance.VeryHigh);
                        }
                    }
                }
            }
        }

        private static bool RValidRange(Obj_AI_Base t)
        {
            var range = GetRealDistance(t);

            if (Config.Item("Rmode").GetValue<StringList>().SelectedIndex == 0)
            {
                if (range > GetRealPowPowRange(t))
                    return true;
                else
                    return false;

            }
            else if (Config.Item("Rmode").GetValue<StringList>().SelectedIndex == 1)
            {
                if (range > Q.Range)
                    return true;
                else
                    return false;
            }
            else if (Config.Item("Rmode").GetValue<StringList>().SelectedIndex == 2)
            {
                if (range > Config.Item("Rcustome").GetValue<Slider>().Value && !Orbwalking.InAutoAttackRange(t))
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        private static void Elogic()
        {
            if (Player.ManaPercent < Config.Item("EmanaCombo").GetValue<Slider>().Value)
                return;

            foreach (var enemy in Enemies.Where(enemy => enemy.IsValidTarget(E.Range) ))
            {
                if(Config.Item("Ecc").GetValue<bool>())
                {
                    if (!CanMove(enemy))
                        E.Cast(enemy.Position);
                    E.CastIfHitchanceEquals(enemy, HitChance.Immobile);
                }

                if(enemy.MoveSpeed < 250 && Config.Item("Eslow").GetValue<bool>())
                    E.Cast(enemy);
                if (Config.Item("Edash").GetValue<bool>())
                    E.CastIfHitchanceEquals(enemy, HitChance.Dashing);
            }
            

            if (Config.Item("Etel").GetValue<bool>())
            {
                foreach (var Object in ObjectManager.Get<Obj_AI_Base>().Where(Obj => Obj.IsEnemy && Obj.Distance(Player.ServerPosition) < E.Range && (Obj.HasBuff("teleport_target", true) || Obj.HasBuff("Pantheon_GrandSkyfall_Jump", true))))
                {
                    E.Cast(Object.Position);
                }
            }

            if (Combo && Player.IsMoving && Config.Item("Ecombo").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget(E.Range) && E.GetPrediction(t).CastPosition.Distance(t.Position) > 200)
                {
                    if (Player.Position.Distance(t.ServerPosition) > Player.Position.Distance(t.Position))
                    {
                        if (t.Position.Distance(Player.ServerPosition) < t.Position.Distance(Player.Position))
                            CastSpell(E, t);
                    }
                    else
                    {
                        if (t.Position.Distance(Player.ServerPosition) > t.Position.Distance(Player.Position))
                            CastSpell(E, t);
                    }
                }
            }
        }

        private static bool WValidRange(Obj_AI_Base t)
        {
            var range = GetRealDistance(t);

            if (Config.Item("Wmode").GetValue<StringList>().SelectedIndex == 0)
            {
                if (range > GetRealPowPowRange(t))
                    return true;
                else
                    return false;

            }
            else if (Config.Item("Wmode").GetValue<StringList>().SelectedIndex == 1)
            {
                if (range > Q.Range)
                    return true;
                else
                    return false;
            }
            else if (Config.Item("Wmode").GetValue<StringList>().SelectedIndex == 2)
            {
                if(range > Config.Item("Wcustome").GetValue<Slider>().Value && !Orbwalking.InAutoAttackRange(t))
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        private static void Wlogic()
        {
            var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget() && WValidRange(t))
            {
                if ( Config.Item("Wks").GetValue<bool>() && GetKsDamage(t,W) > t.Health && ValidUlt(t))
                {
                    CastSpell(W, t);
                }
                else if (Combo && Config.Item("Wcombo").GetValue<bool>() && Player.ManaPercent > Config.Item("WmanaCombo").GetValue<Slider>().Value)
                {
                    CastSpell(W, t);
                }
                else if (Farm && Orbwalking.CanAttack() && !Player.IsWindingUp && Config.Item("Wcombo").GetValue<bool>() && Player.ManaPercent > Config.Item("WmanaHarass").GetValue<Slider>().Value)
                {
                    if (Config.Item("Wts").GetValue<StringList>().SelectedIndex == 0)
                    {
                        if (Config.Item("haras" + t.ChampionName).GetValue<bool>())
                            CastSpell(W, t);
                    }
                    else
                    {
                        foreach (var enemy in Enemies.Where(enemy => enemy.IsValidTarget(W.Range) && WValidRange(t) && Config.Item("haras" + enemy.ChampionName).GetValue<bool>()))
                            CastSpell(W, enemy);
                    }
                }
            }
        }

        private static void Qlogic()
        {
            if (FishBoneActive)
            {
                if(Config.Item("Qchange").GetValue<StringList>().SelectedIndex == 0 && Config.Item("Qcombo").GetValue<bool>() && Orbwalker.GetTarget() != null && Orbwalker.GetTarget() is Obj_AI_Hero)
                {
                    var t = (Obj_AI_Hero)Orbwalker.GetTarget();
                    FishBoneToMiniGun(t);
                }
                else
                {
                    if (Farm && Config.Item("Qharass").GetValue<bool>())
                        Q.Cast();
                }
            }
            else
            {
                var t = TargetSelector.GetTarget(Q.Range + 60, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if ((!Orbwalking.InAutoAttackRange(t) || t.CountEnemiesInRange(250) >= Config.Item("Qaoe").GetValue<Slider>().Value))
                    {
                        if (Combo && Config.Item("Qcombo").GetValue<bool>() && (Player.ManaPercent > Config.Item("QmanaCombo").GetValue<Slider>().Value || Player.GetAutoAttackDamage(t) * Config.Item("QmanaIgnore").GetValue<Slider>().Value > t.Health))
                        {
                            Q.Cast();
                        }
                        if (Farm && Orbwalking.CanAttack() && !Player.IsWindingUp && Config.Item("Qharass").GetValue<bool>() && (Player.ManaPercent > Config.Item("QmanaHarass").GetValue<Slider>().Value || Player.GetAutoAttackDamage(t) * Config.Item("QmanaIgnore").GetValue<Slider>().Value > t.Health))
                        {
                            Q.Cast();
                        }
                    }
                }
                else
                {
                    if (Combo && Player.ManaPercent > Config.Item("QmanaCombo").GetValue<Slider>().Value)
                    {
                        Q.Cast();
                    }
                    else if (Farm && !Player.IsWindingUp && Config.Item("farmQout").GetValue<bool>() && Orbwalking.CanAttack())
                    {
                        foreach (var minion in MinionManager.GetMinions(Q.Range + 30).Where(
                        minion => !Orbwalking.InAutoAttackRange(minion) && minion.Health < Player.GetAutoAttackDamage(minion) * 1.2 && GetRealPowPowRange(minion) < GetRealDistance(minion) && Q.Range < GetRealDistance(minion)))
                        {
                            Orbwalker.ForceTarget(minion);
                            Q.Cast();
                            return;
                        }
                    }
                }
            }
        }

        public static float GetKsDamage(Obj_AI_Base t, Spell QWER)
        {
            var totalDmg = QWER.GetDamage(t);

            if (Player.HasBuff("itemmagicshankcharge"))
            {
                if (Player.GetBuff("itemmagicshankcharge").Count == 100)
                {
                    totalDmg += (float)Player.CalcDamage(t, Damage.DamageType.Magical, 100 + 0.1 * Player.FlatMagicDamageMod);
                }
            }

            if (Player.HasBuff("summonerexhaust"))
                totalDmg = totalDmg * 0.6f;

            if (t.HasBuff("ferocioushowl"))
                totalDmg = totalDmg * 0.7f;

            if (t is Obj_AI_Hero)
            {
                var champion = (Obj_AI_Hero)t;
                if (champion.ChampionName == "Blitzcrank" && !champion.HasBuff("BlitzcrankManaBarrierCD") && !champion.HasBuff("ManaBarrier"))
                {
                    totalDmg -= champion.Mana / 2f;
                }
            }

            var extraHP = t.Health - HealthPrediction.GetHealthPrediction(t, 500);

            totalDmg += extraHP;
            totalDmg -= t.HPRegenRate;
            totalDmg -= t.PercentLifeStealMod * 0.005f * t.FlatPhysicalDamageMod;

            return totalDmg;
        }

        public static bool ValidUlt(Obj_AI_Base target)
        {
            if (target.HasBuffOfType(BuffType.PhysicalImmunity)
                || target.HasBuffOfType(BuffType.SpellImmunity)
                || target.IsZombie
                || target.IsInvulnerable
                || target.HasBuffOfType(BuffType.Invulnerability)
                || target.HasBuffOfType(BuffType.SpellShield)
                || target.HasBuff("deathdefiedbuff")
                || target.HasBuff("Undying Rage")
                || target.HasBuff("Chrono Shift")
                )
                return false;
            else
                return true;
        }

        private static bool CanMove(Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Knockup) ||
                target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Knockback) ||
                target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) ||
                target.IsStunned || target.IsChannelingImportantSpell() || target.MoveSpeed < 50f)
            {
                return false;
            }
            else
                return true;
        }

        private static void CastSpell(Spell QWER, Obj_AI_Base target)
        {
            QWER.Cast(target);
        }

        private static void FishBoneToMiniGun(Obj_AI_Base t)
        {
            var realDistance = GetRealDistance(t);

            if(realDistance < GetRealPowPowRange(t) && t.CountEnemiesInRange(250) < Config.Item("Qaoe").GetValue<Slider>().Value)
            {
                if (Combo && Config.Item("Qcombo").GetValue<bool>() && (Player.ManaPercent < Config.Item("QmanaCombo").GetValue<Slider>().Value || Player.GetAutoAttackDamage(t) * Config.Item("QmanaIgnore").GetValue<Slider>().Value < t.Health))
                    Q.Cast();
                else if (Farm && Config.Item("Qharass").GetValue<bool>() && (Player.ManaPercent < Config.Item("QmanaHarass").GetValue<Slider>().Value || Player.GetAutoAttackDamage(t) * Config.Item("QmanaIgnore").GetValue<Slider>().Value < t.Health))
                    Q.Cast();
            }
        }

        private static float GetRealDistance(Obj_AI_Base target) { return Player.ServerPosition.Distance(target.ServerPosition) + Player.BoundingRadius + target.BoundingRadius; }

        private static float GetRealPowPowRange(GameObject target) { return 650f + Player.BoundingRadius + target.BoundingRadius; }

        private static void SetValues()
        {
            if (Player.AttackRange > 525f)
                FishBoneActive = true;
            else
                FishBoneActive = false;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo = true;
            else
                Combo = false;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Farm = true;
            else
                Farm = false;

            Q.Range = 685f + Player.BoundingRadius + 25f * Player.Spellbook.GetSpell(SpellSlot.Q).Level;

            QMANA = 10f;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
            RMANA = R.Instance.ManaCost;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("qRange").GetValue<bool>())
            {
                if (FishBoneActive)
                    Utility.DrawCircle(Player.Position, 590f + Player.BoundingRadius, System.Drawing.Color.Gray, 1, 1);
                else
                    Utility.DrawCircle(Player.Position, Q.Range - 40, System.Drawing.Color.Gray, 1, 1);
            }
            if (Config.Item("wRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (W.IsReady())
                        Utility.DrawCircle(Player.Position, W.Range, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, W.Range, System.Drawing.Color.Gray, 1, 1);
            }
            if (Config.Item("eRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (E.IsReady())
                        Utility.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Gray, 1, 1);
            }
        }
    }
}
