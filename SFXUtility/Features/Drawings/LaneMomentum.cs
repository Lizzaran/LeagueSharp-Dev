#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 LaneMomentum.cs is part of SFXUtility.

 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXUtility.Classes;
using SFXUtility.Library;
using SFXUtility.Library.Logger;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

#endregion

namespace SFXUtility.Features.Drawings
{
    internal class LaneMomentum : Child<Drawings>
    {
        private const float CheckInterval = 1000f;
        private const float MaxDistance = 3000f;
        private const int MaxWeight = 10;
        private readonly List<Obj_AI_Minion> _botChaosMinions = new List<Obj_AI_Minion>();
        private readonly List<Obj_AI_Minion> _botOrderMinions = new List<Obj_AI_Minion>();
        private readonly Geometry.Polygon _botRegion = new Geometry.Polygon();
        private readonly List<Obj_AI_Minion> _midChaosMinions = new List<Obj_AI_Minion>();
        private readonly List<Obj_AI_Minion> _midOrderMinions = new List<Obj_AI_Minion>();
        private readonly Geometry.Polygon _midRegion = new Geometry.Polygon();
        private readonly List<Obj_AI_Minion> _topChaosMinions = new List<Obj_AI_Minion>();
        private readonly List<Obj_AI_Minion> _topOrderMinions = new List<Obj_AI_Minion>();
        private readonly Geometry.Polygon _topRegion = new Geometry.Polygon();
        private int _botChaos;
        private int _botOrder;
        private float _lastCheck = Environment.TickCount;
        private Line _line;
        private int _midChaos;
        private int _midOrder;
        private int _topChaos;
        private int _topOrder;
        private int _weightMelee;
        private int _weightRanged;
        private int _weightSiege;
        private int _weightSuper;

        public LaneMomentum(Drawings parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Lane Momentum"; }
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                {
                    return;
                }

                if (_line != null && !_line.IsDisposed)
                {
                    var colorChaos = Menu.Item(Name + "DrawingColorChaos").GetValue<Color>();
                    var colorOrder = Menu.Item(Name + "DrawingColorOrder").GetValue<Color>();
                    var alpha = (byte) (Menu.Item(Name + "DrawingOpacity").GetValue<Slider>().Value * 255 / 100);

                    var width = Menu.Item(Name + "DrawingWidth").GetValue<Slider>().Value;
                    var height = Menu.Item(Name + "DrawingHeight").GetValue<Slider>().Value;

                    var sColorChaos = new ColorBGRA(colorChaos.R, colorChaos.G, colorChaos.B, alpha);
                    var sColorOrder = new ColorBGRA(colorOrder.R, colorOrder.G, colorOrder.B, alpha);
                    var sColorBorder = new ColorBGRA(255, 255, 255, alpha);

                    var posX = Menu.Item(Name + "DrawingOffsetLeft").GetValue<Slider>().Value;
                    var posY = Menu.Item(Name + "DrawingOffsetTop").GetValue<Slider>().Value;

                    var maxWeight = Menu.Item(Name + "MaxWeight").GetValue<Slider>().Value;

                    var margin = width / 3f * 0.075f;
                    var barWidth = (width - margin * 2f) / 3f;
                    var barHeight = height;

                    var offset = 0f;

                    for (var i = 0; i < 3; i++)
                    {
                        var difference = (i == 0
                            ? _topChaos - _topOrder
                            : (i == 1 ? _midChaos - _midOrder : _botChaos - _botOrder));
                        var chaosWins = difference > 0;
                        difference = Math.Abs(difference);
                        if (difference > 0)
                        {
                            var pWidth = (barWidth / 2f) / maxWeight * Math.Min(maxWeight, difference);
                            _line.Width = barHeight;
                            _line.Begin();
                            if (chaosWins)
                            {
                                _line.Draw(
                                    new[]
                                    {
                                        new Vector2(posX + offset + barWidth / 2f - pWidth, posY + barHeight / 2f),
                                        new Vector2(posX + offset + barWidth / 2f, posY + barHeight / 2f)
                                    }, sColorChaos);
                            }
                            else
                            {
                                _line.Draw(
                                    new[]
                                    {
                                        new Vector2(posX + offset + barWidth / 2f, posY + barHeight / 2f),
                                        new Vector2(posX + offset + barWidth / 2f + pWidth, posY + barHeight / 2f)
                                    },
                                    sColorOrder);
                            }
                            _line.End();
                        }

                        _line.Width = 1;
                        _line.Begin();

                        _line.Draw(
                            new[] { new Vector2(posX + offset, posY), new Vector2(posX + offset + barWidth, posY) },
                            sColorBorder);
                        _line.Draw(
                            new[]
                            {
                                new Vector2(posX + offset + barWidth, posY),
                                new Vector2(posX + offset + barWidth, posY + barHeight)
                            }, sColorBorder);
                        _line.Draw(
                            new[]
                            {
                                new Vector2(posX + offset + (barWidth / 2f - 0.5f), posY),
                                new Vector2(posX + offset + (barWidth / 2f - 0.5f), posY + barHeight)
                            }, sColorBorder);
                        _line.Draw(
                            new[]
                            {
                                new Vector2(posX + offset, posY + barHeight),
                                new Vector2(posX + offset + barWidth, posY + barHeight)
                            }, sColorBorder);
                        _line.Draw(
                            new[] { new Vector2(posX + offset, posY), new Vector2(posX + offset, posY + barHeight) },
                            sColorBorder);

                        _line.End();

                        offset += barWidth + margin;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnEndScene += OnDrawingEndScene;
            GameObject.OnCreate += OnGameObjectCreate;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
            Drawing.OnEndScene -= OnDrawingEndScene;
            GameObject.OnCreate -= OnGameObjectCreate;
            base.OnDisable();
        }

        protected override sealed void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);

                var drawingMenu = new Menu("Drawing", Name + "Drawing");
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "ColorChaos", "Color Chaos").SetValue(Color.DarkRed));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "ColorOrder", "Color Order").SetValue(Color.DeepSkyBlue));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Opacity", "Opacity").SetValue(new Slider(60, 5)));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Height", "Height").SetValue(new Slider(15, 10)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "Width", "Width").SetValue(new Slider(300, 50, 1000)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "OffsetTop", "Offset Top").SetValue(
                        new Slider(5, 0, Drawing.Height)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "OffsetLeft", "Offset Left").SetValue(
                        new Slider((int) (Drawing.Width / 2f - 150), 0, Drawing.Width)));

                Menu.AddSubMenu(drawingMenu);

                var weightsMenu = new Menu("Weights", Name + "Weights");
                weightsMenu.AddItem(
                    new MenuItem(weightsMenu.Name + "Melee", "Melee").SetValue(
                        new Slider(MaxWeight / MaxWeight, 1, MaxWeight))).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        _weightMelee = args.GetNewValue<Slider>().Value;
                    };
                weightsMenu.AddItem(
                    new MenuItem(weightsMenu.Name + "Ranged", "Ranged").SetValue(
                        new Slider(MaxWeight / MaxWeight * 2, 1, MaxWeight))).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        _weightRanged = args.GetNewValue<Slider>().Value;
                    };
                weightsMenu.AddItem(
                    new MenuItem(weightsMenu.Name + "Siege", "Siege").SetValue(
                        new Slider(MaxWeight / MaxWeight * 4, 1, MaxWeight))).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        _weightSiege = args.GetNewValue<Slider>().Value;
                    };
                weightsMenu.AddItem(
                    new MenuItem(weightsMenu.Name + "Super", "Super").SetValue(new Slider(MaxWeight, 1, MaxWeight)))
                    .ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        _weightSuper = args.GetNewValue<Slider>().Value;
                    };

                Menu.AddSubMenu(weightsMenu);

                Menu.AddItem(
                    new MenuItem(Menu.Name + "MaxWeight", "Max. Weight").SetValue(
                        new Slider(MaxWeight * 2, MaxWeight, MaxWeight * 5)));

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                _line = MDrawing.GetLine(1);

                Parent.Menu.AddSubMenu(Menu);

                _weightMelee = Menu.Item(Name + "WeightsMelee").GetValue<Slider>().Value;
                _weightRanged = Menu.Item(Name + "WeightsRanged").GetValue<Slider>().Value;
                _weightSiege = Menu.Item(Name + "WeightsSiege").GetValue<Slider>().Value;
                _weightSuper = Menu.Item(Name + "WeightsSuper").GetValue<Slider>().Value;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnInitialize()
        {
            try
            {
                if (Utility.Map.GetMap().Type != Utility.Map.MapType.SummonersRift)
                {
                    OnUnload(null, new UnloadEventArgs(true));
                    return;
                }

                #region Regions

                _topRegion.Add(new Vector3(1576.24f, 1902.23f, 94.70f));
                _topRegion.Add(new Vector3(1661.70f, 2289.45f, 95.75f));
                _topRegion.Add(new Vector3(2380.17f, 4091.17f, 95.75f));
                _topRegion.Add(new Vector3(2725.03f, 5469.05f, 52.67f));
                _topRegion.Add(new Vector3(2515.89f, 10313.13f, 54.33f));
                _topRegion.Add(new Vector3(4407.59f, 11827.64f, 55.64f));
                _topRegion.Add(new Vector3(4876.42f, 12259.18f, 56.48f));
                _topRegion.Add(new Vector3(9414.86f, 12181.32f, 52.31f));
                _topRegion.Add(new Vector3(10870.46f, 12646.18f, 91.43f));
                _topRegion.Add(new Vector3(13060.38f, 13277.48f, 92.83f));
                _topRegion.Add(new Vector3(13900.90f, 13907.86f, 148.66f));
                _topRegion.Add(new Vector3(13614.24f, 14538.74f, 154.61f));
                _topRegion.Add(new Vector3(3099.85f, 14303.65f, 52.84f));
                _topRegion.Add(new Vector3(1314.28f, 13313.79f, 54.59f));
                _topRegion.Add(new Vector3(734.45f, 12169.86f, 52.84f));
                _topRegion.Add(new Vector3(430.93f, 10373.04f, 52.84f));
                _topRegion.Add(new Vector3(435.87f, 4959.91f, 131.13f));
                _topRegion.Add(new Vector3(242.55f, 4269.17f, 93.61f));
                _topRegion.Add(new Vector3(292.16f, 1174.47f, 146.87f));
                _topRegion.Add(new Vector3(931.74f, 999.36f, 151.49f));
                _topRegion.Add(new Vector3(1576.24f, 1902.23f, 94.70f));

                _midRegion.Add(new Vector3(13871.14f, 14021.69f, 155.72f));
                _midRegion.Add(new Vector3(13008.55f, 13299.83f, 91.88f));
                _midRegion.Add(new Vector3(10887.83f, 12486.93f, 91.43f));
                _midRegion.Add(new Vector3(9197.48f, 11867.53f, 52.31f));
                _midRegion.Add(new Vector3(4857.02f, 8788.97f, -65.16f));
                _midRegion.Add(new Vector3(2903.06f, 5427.32f, 52.45f));
                _midRegion.Add(new Vector3(2554.77f, 3976.37f, 95.75f));
                _midRegion.Add(new Vector3(1819.52f, 2226.85f, 95.75f));
                _midRegion.Add(new Vector3(1504.61f, 1763.40f, 93.38f));
                _midRegion.Add(new Vector3(866.45f, 1057.75f, 140.08f));
                _midRegion.Add(new Vector3(1087.19f, 674.40f, 157.85f));
                _midRegion.Add(new Vector3(1875.13f, 1649.40f, 95.75f));
                _midRegion.Add(new Vector3(2201.99f, 1856.39f, 95.75f));
                _midRegion.Add(new Vector3(4020.46f, 2469.27f, 95.75f));
                _midRegion.Add(new Vector3(6070.94f, 3567.61f, 50.33f));
                _midRegion.Add(new Vector3(9287.77f, 5912.96f, -71.24f));
                _midRegion.Add(new Vector3(10487.25f, 7606.01f, 51.67f));
                _midRegion.Add(new Vector3(12330.79f, 11017.61f, 91.43f));
                _midRegion.Add(new Vector3(13027.33f, 12675.33f, 93.34f));
                _midRegion.Add(new Vector3(13174.45f, 13057.41f, 91.83f));
                _midRegion.Add(new Vector3(14089.08f, 13842.00f, 153.87f));
                _midRegion.Add(new Vector3(13871.14f, 14021.69f, 155.72f));

                _botRegion.Add(new Vector3(1059.34f, 746.28f, 156.04f));
                _botRegion.Add(new Vector3(2203.51f, 1795.44f, 95.75f));
                _botRegion.Add(new Vector3(4059.19f, 2374.68f, 95.75f));
                _botRegion.Add(new Vector3(7394.06f, 3361.99f, 52.59f));
                _botRegion.Add(new Vector3(9484.03f, 3413.96f, 60.04f));
                _botRegion.Add(new Vector3(11380.81f, 4031.34f, -71.24f));
                _botRegion.Add(new Vector3(11368.18f, 7687.61f, 52.22f));
                _botRegion.Add(new Vector3(12474.98f, 10944.70f, 91.43f));
                _botRegion.Add(new Vector3(13262.80f, 12976.91f, 91.43f));
                _botRegion.Add(new Vector3(13990.51f, 13894.96f, 153.36f));
                _botRegion.Add(new Vector3(14577.87f, 13772.93f, 157.70f));
                _botRegion.Add(new Vector3(14427.08f, 4090.74f, 52.48f));
                _botRegion.Add(new Vector3(13674.34f, 2334.65f, 51.37f));
                _botRegion.Add(new Vector3(12273.99f, 886.75f, 51.27f));
                _botRegion.Add(new Vector3(11367.76f, 586.92f, 50.59f));
                _botRegion.Add(new Vector3(1077.09f, 289.64f, 155.13f));
                _botRegion.Add(new Vector3(1059.34f, 746.28f, 156.04f));

                #endregion

                foreach (var minion in GameObjects.Minions)
                {
                    OnMinionCreated(minion);
                }

                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameObjectCreate(GameObject sender, EventArgs args)
        {
            try
            {
                var minion = sender as Obj_AI_Minion;
                if (minion != null)
                {
                    OnMinionCreated(minion);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnMinionCreated(Obj_AI_Minion minion)
        {
            try
            {
                if (minion.IsValid && !minion.IsDead)
                {
                    if (_midRegion.IsInside(minion))
                    {
                        if (minion.Team == GameObjectTeam.Chaos)
                        {
                            _midChaosMinions.Add(minion);
                        }
                        else if (minion.Team == GameObjectTeam.Order)
                        {
                            _midOrderMinions.Add(minion);
                        }
                    }
                    else if (_topRegion.IsInside(minion))
                    {
                        if (minion.Team == GameObjectTeam.Chaos)
                        {
                            _topChaosMinions.Add(minion);
                        }
                        else if (minion.Team == GameObjectTeam.Order)
                        {
                            _topOrderMinions.Add(minion);
                        }
                    }
                    else if (_botRegion.IsInside(minion))
                    {
                        if (minion.Team == GameObjectTeam.Chaos)
                        {
                            _botChaosMinions.Add(minion);
                        }
                        else if (minion.Team == GameObjectTeam.Order)
                        {
                            _botOrderMinions.Add(minion);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (_lastCheck + CheckInterval > Environment.TickCount)
                {
                    return;
                }

                _lastCheck = Environment.TickCount;

                var chaosStart = GameObjects.SpawnPoints.First(s => s.Team == GameObjectTeam.Chaos).Position;
                var orderStart = GameObjects.SpawnPoints.First(s => s.Team == GameObjectTeam.Order).Position;

                _topChaosMinions.RemoveAll(m => !m.IsValid || m.IsDead);
                _topOrderMinions.RemoveAll(m => !m.IsValid || m.IsDead);

                _midChaosMinions.RemoveAll(m => !m.IsValid || m.IsDead);
                _midOrderMinions.RemoveAll(m => !m.IsValid || m.IsDead);

                _botChaosMinions.RemoveAll(m => !m.IsValid || m.IsDead);
                _botOrderMinions.RemoveAll(m => !m.IsValid || m.IsDead);

                var topChaosMinion = _topChaosMinions.OrderBy(m => m.Distance(orderStart)).FirstOrDefault();
                var topOrderMinion = _topOrderMinions.OrderBy(m => m.Distance(chaosStart)).FirstOrDefault();

                var midChaosMinion = _midChaosMinions.OrderBy(m => m.Distance(orderStart)).FirstOrDefault();
                var midOrderMinion = _midOrderMinions.OrderBy(m => m.Distance(chaosStart)).FirstOrDefault();

                var botChaosMinion = _botChaosMinions.OrderBy(m => m.Distance(orderStart)).FirstOrDefault();
                var botOrderMinion = _botOrderMinions.OrderBy(m => m.Distance(chaosStart)).FirstOrDefault();

                _topChaos =
                    _topChaosMinions.Where(m => topOrderMinion == null || m.Distance(topOrderMinion) <= MaxDistance)
                        .Sum(m => GetPoints(m));

                _topOrder =
                    _topOrderMinions.Where(m => topChaosMinion == null || m.Distance(topChaosMinion) <= MaxDistance)
                        .Sum(m => GetPoints(m));

                _midChaos =
                    _midChaosMinions.Where(m => midOrderMinion == null || m.Distance(midOrderMinion) <= MaxDistance)
                        .Sum(m => GetPoints(m));

                _midOrder =
                    _midOrderMinions.Where(m => midChaosMinion == null || m.Distance(midChaosMinion) <= MaxDistance)
                        .Sum(m => GetPoints(m));

                _botChaos =
                    _botChaosMinions.Where(m => botOrderMinion == null || m.Distance(botOrderMinion) <= MaxDistance)
                        .Sum(m => GetPoints(m));

                _botOrder =
                    _botOrderMinions.Where(m => botChaosMinion == null || m.Distance(botChaosMinion) <= MaxDistance)
                        .Sum(m => GetPoints(m));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private int GetPoints(Obj_AI_Minion minion)
        {
            try
            {
                return minion.CharData.BaseSkinName.Contains("Melee")
                    ? _weightMelee
                    : (minion.CharData.BaseSkinName.Contains("Ranged")
                        ? _weightRanged
                        : (minion.CharData.BaseSkinName.Contains("Super")
                            ? _weightSuper
                            : (minion.CharData.BaseSkinName.Contains("Siege") ? _weightSiege : 0)));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }
    }
}