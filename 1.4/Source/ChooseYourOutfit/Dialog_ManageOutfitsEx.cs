using ChooseYourOutfit;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HarmonyLib.Code;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Security.Cryptography;
using Verse.Sound;
using Verse.Noise;

namespace ChooseYourOutfit
{
    public class Dialog_ManageOutfitsEx : Dialog_ManageOutfits
    {
        //選択されたポーンを受け取ってOutfit情報だけをDialog_ManageOutfitsのコンストラクタに渡す
        public Dialog_ManageOutfitsEx(Pawn selectedPawn) : base(selectedPawn?.outfits.CurrentOutfit)
        {
            apparelsScrollPosition = default;
            listScrollPosition = default;
            SelectedPawn = selectedPawn;
            selQualityButtonLabel = QualityCategory.Normal.ToString();
            foreach(var gender in Enum.GetValues(typeof(Gender)).Cast<Gender>())
            {
                svg.Add(gender, XDocument.Load(ModLister.GetActiveModWithIdentifier("oels.chooseyouroutfit", false).RootDir.FullName + @"/ButtonColliders/Male.svg"));
            }

            rect5 = new Rect(310f, 40f, 320f, InitialSize.y - Margin * 2 - 40f - Window.CloseButSize.y).ContractedBy(10f);
            rect6 = new Rect(310f + rect5.width + 10f, 40f, InitialSize.x - Margin * 2 - 310f - rect5.width - 320f, rect5.height + 5f).ContractedBy(10f);

            if (SelectedPawn == null)
            {
                selPawnButtonLabel = "AnyColonist".Translate().ToString();
                SVGInterpreter.GetViewBox(svg[Gender.None]);
                buttonColliders = SVGInterpreter.SVGToPolygons(svg[Gender.None], rect6);
            }
            else
            {
                selPawnButtonLabel = selectedPawn.LabelShortCap;
                SVGInterpreter.GetViewBox(svg[SelectedPawn.gender]);
                buttonColliders = SVGInterpreter.SVGToPolygons(svg[SelectedPawn.gender], rect6);
            }
        }

        private Pawn SelectedPawn
        {
            get
            {
                return this.selPawnInt;
            }
            set
            {
                this.selPawnInt = value;
            }
        }

        private List<ApparelLayerDef> SelectedLayers
        {
            get
            {
                return this.selLayersInt;
            }
            set
            {
                this.selLayersInt = value;
            }
        }
        private List<ThingDef> SelectedApparels
        {
            get
            {
                return this.selApparelsInt;
            }
            set
            {
                this.selApparelsInt = value;
            }
        }

        private List<BodyPartGroupDef> SelectedBodypartGroups
        {
            get
            {
                return this.selBodyPartGroupsInt;
            }
            set
            {
                this.selBodyPartGroupsInt = value;
            }
        }

        //ManageOutfitsダイアログのウィンドウサイズを変更
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1400f, 700f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);

            //baseのDoWindowContentsメソッドの後に追加の衣装選択インターフェイスを描画する
            var selectedOutfit = AccessTools.FieldRefAccess<Outfit>(typeof(Dialog_ManageOutfits), "selOutfitInt")(this);
            if (selectedOutfit == null) return;

            SelectedApparels = selectedOutfit.filter?.AllowedThingDefs?.ToList();

            Widgets.BeginGroup(rect5);

            //入植者選択ボタン
            Widgets.Dropdown(new Rect(0f, 0f, 150f, 35f),
                null,
                null,
                (Pawn p) => GeneratePawnList(p),
                selPawnButtonLabel,
                null,
                null,
                null,
                null,
                true);


            //レイヤー選択ビュー
            var layerViewHeight = DoLayers(new Vector2(0f, 40f), 300f);
            //服選択ビュー
            DoApparels(new Rect(0f, layerViewHeight + 10f + 40f, 300f, rect5.height - layerViewHeight - 40f - 25f));
            Widgets.EndGroup();

            //選択したapparelのリストを描画
            var viewBox = SVGInterpreter.GetViewBox(svg[SelectedPawn?.gender ?? Gender.None]);
            var scale = rect6.height / viewBox.height;
            Rect rect8 = new Rect(rect6.x, rect6.y, rect6.width - viewBox.width * scale, rect6.height);
            Widgets.BeginGroup(rect8);
            DoOutfitPreview(new Rect(0f, 0f, rect8.width, rect8.width));
            DoSelectedApparelList(new Rect(0f, rect8.width + 10f, rect8.width, rect8.height - rect8.width - 10f));
            Widgets.EndGroup();

            //ポーンの体を描画するとこ
            Widgets.BeginGroup(rect6);
            DoPawnBodySeparatedByParts(rect6.AtZero());
            Widgets.EndGroup();


            //右のインフォカード描画
            Rect rect7 = new Rect(inRect.xMax - 320f, 40f, 320f, rect5.height + 5f).ContractedBy(10f);
            Widgets.BeginGroup(rect7);
            DoInfoCard(rect7.AtZero());
            Widgets.EndGroup();

            selectedOutfit.filter.SetDisallowAll();
            SelectedApparels.ForEach(a => selectedOutfit.filter.SetAllow(a, true));
        }

        //ドロップダウンメニューのポーンリストを生成
        public IEnumerable<Widgets.DropdownMenuElement<Pawn>> GeneratePawnList(Pawn pawn)
        {
            yield return new Widgets.DropdownMenuElement<Pawn>
            {
                option = new FloatMenuOption("AnyColonist".Translate(), delegate ()
                {
                    SelectedPawn = null;
                    selPawnButtonLabel = "AnyColonist".Translate();
                    buttonColliders = SVGInterpreter.SVGToPolygons(svg[Gender.None], rect6);
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                payload = pawn
            };

            foreach (var entry in Find.ColonistBar.Entries)
            {
                yield return new Widgets.DropdownMenuElement<Pawn>
                {
                    option = new FloatMenuOption(entry.pawn.LabelShortCap, delegate ()
                    {
                        SelectedPawn = entry.pawn;
                        selPawnButtonLabel = SelectedPawn.LabelShortCap;
                        buttonColliders = SVGInterpreter.SVGToPolygons(svg[SelectedPawn.gender], rect6);
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = pawn
                };
            }
        }

        //クオリティリストを生成
        public IEnumerable<Widgets.DropdownMenuElement<QualityCategory>> GenerateQualityList(QualityCategory quality)
        {
            foreach (var cat in QualityUtility.AllQualityCategories)
            {
                yield return new Widgets.DropdownMenuElement<QualityCategory>
                {
                    option = new FloatMenuOption(cat.GetLabel(), delegate ()
                    {
                        selQualityInt = cat;
                        selQualityButtonLabel = cat.GetLabel();
                        CYO_StatsReporter.Reset();
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = quality
                };
            }
        }

        //素材リストを生成
        public IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GenerateStuffList(ThingDef tDef)
        {
            foreach (var stuff in GenStuff.AllowedStuffsFor(statsDrawn))
            {
                yield return new Widgets.DropdownMenuElement<ThingDef>
                {
                    option = new FloatMenuOption(stuff.LabelAsStuff, delegate ()
                    {
                        selStuffList.Replace(selStuffInt, stuff);
                        selStuffInt = stuff;
                        selStuffButtonLabel = stuff.LabelAsStuff;
                        CYO_StatsReporter.Reset();
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = tDef
                };
            }
        }

        //服のレイヤーリストを描画
        public float DoLayers(Vector2 pos, float width)
        {
            List<ApparelLayerDef> layers = DefDatabase<ApparelLayerDef>.AllDefsListForReading
                .Where(l => DefDatabase<ThingDef>.AllDefsListForReading //以下で選択したbodypartgroupが着られるapparelを持つlayerに限定
                .Where(a => a.apparel?.bodyPartGroups.Any(g => SelectedBodypartGroups?.Contains(g) ?? true) ?? false)
                .Any(a => a.apparel.layers.Contains(l))).ToList();

            if(layers.Count == 0)
            {
                Rect rect = new Rect(pos.x, pos.y, width, Text.LineHeight);
                Widgets.Label(rect, "NoApparels".Translate());
                return rect.height;
            }

            Rect itemRect = new Rect(pos.x, pos.y, width, Text.LineHeight);
            Rect outerRect = itemRect;
            outerRect.height += Text.LineHeight * layers.Count;
            Widgets.DrawMenuSection(outerRect);

            layers.SortByDescending(l => l.drawOrder);
            Widgets.Label(new Rect(itemRect.position + new Vector2(20f, 0f), itemRect.size), "AllLayers".Translate());
            if (Widgets.ButtonInvisible(itemRect, true)) SelectedLayers = DefDatabase<ApparelLayerDef>.AllDefsListForReading;
            Widgets.DrawHighlightIfMouseover(itemRect);
            itemRect.y += Text.LineHeight;

            if (!SelectedLayers.Any(l => layers.Contains(l)))
            {
                SelectedLayers = new List<ApparelLayerDef>
                {
                    layers.Last()
                };
            }

            Text.Font = GameFont.Small;
            foreach (var layer in layers)
            {
                if(Widgets.ButtonInvisible(itemRect, true))
                {
                    SelectedLayers = new List<ApparelLayerDef> { layer };
                }
                if(SelectedLayers.Contains(layer)) Widgets.DrawHighlightSelected(itemRect);
                Widgets.DrawHighlightIfMouseover(itemRect);
                Rect offsetRect = itemRect;
                offsetRect.xMin += 20f;
                Widgets.Label(offsetRect, layer.label.Truncate(offsetRect.width - 20f));
                itemRect.y += Text.LineHeight;
            }
            return outerRect.height;
        }

        //pawnが着られる現在のレイヤーの服のリストを描画
        public void DoApparels (Rect outerRect)
        {
            if (SelectedLayers.Count == 0) return;
            IEnumerable<ThingDef> apparels = DefDatabase<ThingDef>.AllDefs
                .Where(a => SelectedLayers.Any(l => a.apparel?.layers.Contains(l) ?? false)) 
                .Where(a => a.apparel?.bodyPartGroups.Any(g => SelectedBodypartGroups?.Contains(g) ?? true) ?? false);

            if (apparels.Count() == 0) return;

            if (SelectedPawn != null) apparels = apparels.Where(d => d.apparel?.PawnCanWear(SelectedPawn) ?? false);
            //条件によってapparelsをグループ化
            var apparelsGrouped = apparels.GroupBy(a => SelectedApparels.Any(s => a.Equals(s)) || //その服が選択されていればtrue
            SelectedApparels.All(s => ApparelUtility.CanWearTogether(a, s, SelectedPawn.RaceProps.body)) && //その服が選択されている全ての服と一緒に着られるならtrue
            a.apparel.bodyPartGroups.Any(b => SelectedPawn?.health.hediffSet.GetNotMissingParts().Any(p => p.groups.Contains(b)) ?? true)); //その服のbodyPartGroupのいずれかをpawnが持っていればtrue
            //Keyがtrueだったグループ→falseだったグループに並び替え
            apparelsGrouped = apparelsGrouped.OrderByDescending(g => g.Key is true);

            Rect itemRect = outerRect;
            itemRect.height = Text.LineHeight;
            Rect viewRect = outerRect;
            viewRect.width -= GenUI.ScrollBarWidth;
            viewRect.height = Text.LineHeight * apparels.Count();
            mouseovered2 = null;

            Widgets.DrawMenuSection(outerRect);
            Widgets.BeginScrollView(outerRect, ref apparelsScrollPosition, viewRect, true);
            foreach (var group in apparelsGrouped)
            {
                foreach (var apparel in group)
                {
                    if (!group.Key) GUI.DrawTexture(itemRect, SolidColorMaterials.NewSolidColorTexture(new Color(0f, 0f, 0f, 0.3f)));
                    if (SelectedApparels?.Contains(apparel) ?? false) Widgets.DrawHighlightSelected(itemRect);
                    Widgets.DrawHighlightIfMouseover(itemRect);
                    if(Mouse.IsOver(itemRect)) mouseovered2 = mouseovered = apparel;
                    Rect offsetRect = itemRect;
                    offsetRect.xMin += 20f;
                    Widgets.DefIcon(new Rect(offsetRect.x, offsetRect.y, Text.LineHeight, offsetRect.height), apparel);
                    offsetRect.xMin += Text.LineHeight + 5f;
                    Widgets.Label(offsetRect, apparel.label.Truncate(offsetRect.width - 20f - Text.LineHeight - 5f));
                    offsetRect.xMax -= 20f;
                    offsetRect.xMin = offsetRect.xMax - Text.LineHeight;
                    offsetRect = offsetRect.ContractedBy(Text.LineHeight * 0.1f);
                    TinyInfoButton(offsetRect, apparel,  GenStuff.DefaultStuffFor(apparel));
                    if (Widgets.ButtonInvisible(itemRect, true))
                    {
                        if (SelectedApparels?.Contains(apparel) ?? false)
                        {
                            SelectedApparels.Remove(apparel);
                        }
                        else
                        {
                            SelectedApparels.Add(apparel);
                        }
                    }
                    TooltipHandler.TipRegion(itemRect, apparel.DescriptionDetailed);
                    itemRect.y += Text.LineHeight;
                }
            }
            Widgets.EndScrollView();
        }

        //パーツで分かれたポーンの体を描画
        public void DoPawnBodySeparatedByParts(Rect rect)
        {
            if (SelectedPawn == null) return;
            
            var mousePosition = Event.current.mousePosition;
            var isInAnyPolygon = false;

            foreach (var (id, polygons) in buttonColliders)
            {
                var part = SelectedPawn.health.hediffSet.GetNotMissingParts()
                    .FirstOrDefault(p => id == p.Label.Replace(" ", "_") || id == p.def.defName);
                if (part == null) continue;

                var groups = part.groups;
                groups.AddRange(DefDatabase<BodyPartGroupDef>.AllDefsListForReading.Where(g => part.Label.Replace(" ", "").EqualsIgnoreCase(g.defName)));

                var pos = new Vector2(polygons.Min(p => p.Min(v => v.x)), polygons.Min(p => p.Min(v => v.y)));
                var size = new Vector2(polygons.Max(p => p.Max(v => v.x)), polygons.Max(p => p.Max(v => v.y))) - pos;

                var unfilledPart = ContentFinder<Texture2D>.Get("ChooseYourOutfit/Body/" + SelectedPawn.gender + "/Unfilled/" + id);
                var filledPart = ContentFinder<Texture2D>.Get("ChooseYourOutfit/Body/" + SelectedPawn.gender + "/Filled/" + id);
                var isInPolygon = polygons.Any(p => PolygonCollider.IsInPolygon(p, mousePosition));

                if (isInPolygon)
                {
                    isInAnyPolygon = true;
                    highlightedGroups = groups;
                    if (Input.GetMouseButton(0))
                    {
                        SelectedBodypartGroups = groups;
                    }
                }

                var partHasSelGroups = SelectedBodypartGroups?.All(p => groups.Contains(p)) ?? false;
                var partHasHlGroups = highlightedGroups?.All(p => groups.Contains(p)) ?? false;
                var partHasHlApGroups = mouseovered2 == null ? false : mouseovered2.apparel.bodyPartGroups.Intersect(part.groups).Count() != 0;
                var color = partHasSelGroups ? new Color(0f, 0.5f, 1f, 1f) : Color.white;

                //このパーツが着ることのできる衣服がある全てのレイヤー
                var allLayers = SelectedLayers
                    .Where(l => DefDatabase<ThingDef>.AllDefs
                    .Where(a => a.apparel?.layers.Contains(l) ?? false)
                    .Any(a => groups.Any(g => a.apparel.bodyPartGroups.Contains(g))));
                //このパーツが衣服を着ているレイヤー
                var wearLayers = SelectedLayers
                    .Where(l => SelectedApparels
                    .Where(a => a.apparel?.layers.Contains(l) ?? false)
                    .Any(a => groups.Any(g => a.apparel.bodyPartGroups.Contains(g))));

                var alpha = new Color(1f, 1f, 1f, allLayers.Count() != 0 ? (float)wearLayers.Count() / (float)allLayers.Count() : 0f);

                GUI.DrawTexture(new Rect(pos, size), filledPart, ScaleMode.ScaleToFit, true, 0f, color * alpha, 0f, 0f);

                GUI.DrawTexture(new Rect(pos, size), unfilledPart, ScaleMode.ScaleToFit, true, 0f, color, 0f, 0f);

                if (!partHasHlGroups)
                    GUI.DrawTexture(new Rect(pos, size), filledPart, ScaleMode.ScaleToFit, true, 0f, Widgets.WindowBGFillColor * new Color(1f, 1f, 1f, 0.5f), 0f, 0f);

                if (partHasHlApGroups)
                    GUI.DrawTexture(new Rect(pos, size), filledPart, ScaleMode.ScaleToFit, true, 0f, new Color(1f, 1f, 0.5f, 0.2f), 0f, 0f);
            }

            if (!isInAnyPolygon) highlightedGroups = null;
            if (Widgets.ButtonInvisible(rect) && !isInAnyPolygon) SelectedBodypartGroups = null;
        }

        public void DoInfoCard(Rect rect)
        {
            Widgets.Dropdown(new Rect(0f, 0f, 145f, 35f),
                selQualityInt,
                null,
                (QualityCategory q) => GenerateQualityList(q),
                selQualityButtonLabel,
                null,
                null,
                null,
                null,
                true);

            if (mouseovered != statsDrawn)
            {
                statsDrawn = mouseovered;
                CYO_StatsReporter.Reset();
            }

            Rect rect2 = new Rect(0f, 40f, rect.width, rect.height - 40f);
            Widgets.DrawMenuSection(rect2);
            if (statsDrawn != null)
            {
                var selStuffInThisThing = GenStuff.AllowedStuffsFor(statsDrawn)?.Intersect(selStuffList)?.FirstOrDefault();

                if (GenStuff.AllowedStuffsFor(statsDrawn).Count() != 0)
                {
                    if (selStuffInThisThing != null)
                    {
                        selStuffInt = selStuffInThisThing;
                        selStuffButtonLabel = selStuffInt.LabelAsStuff;
                    }
                    else
                    {
                        selStuffInt = GenStuff.DefaultStuffFor(statsDrawn);
                        selStuffList.Add(selStuffInt);
                        selStuffButtonLabel = selStuffInt.LabelAsStuff;
                    }
                    Widgets.Dropdown(new Rect(155f, 0f, 145f, 35f),
                        null,
                        null,
                        (ThingDef s) => GenerateStuffList(s),
                        selStuffButtonLabel,
                        null,
                        null,
                        null,
                        null,
                        true);
                }

                CYO_StatsReporter.DrawStatsReport(rect2.ContractedBy(10f), statsDrawn, selStuffInt, selQualityInt);
            }
        }

        public void DoSelectedApparelList(Rect outerRect)
        {
            apparelListViewRect.x = outerRect.x;
            apparelListViewRect.width = outerRect.width - GenUI.ScrollBarWidth;
            apparelListViewRect.yMin = Math.Min(outerRect.yMin, outerRect.yMin + outerRect.yMax - apparelListViewRect.yMax);
            apparelListViewRect.yMax = outerRect.yMax;
            Rect itemRect = apparelListViewRect;
            itemRect.y = outerRect.yMax + 3f;
            itemRect.height = Text.LineHeight;

            SelectedApparels = SelectedApparels.OrderBy(s => s.apparel.layers.Max(l => l.drawOrder)).ToList();
            List<ThingDef> drawn = new List<ThingDef>();

            Widgets.BeginScrollView(outerRect, ref listScrollPosition, apparelListViewRect, true);
            foreach (var apparel in SelectedApparels.ToArray())
            {
                if (drawn.Contains(apparel)) continue;

                itemRect.y -= 3f;
                Widgets.DrawLineHorizontal(itemRect.x, itemRect.y + 1.5f, itemRect.width);

                itemRect.y -= Text.LineHeight;

                Widgets.DrawHighlightIfMouseover(itemRect);
                Widgets.Label(itemRect, apparel.label.Truncate(itemRect.width));
                if (Widgets.ButtonInvisible(itemRect))
                {
                    SelectedApparels.Remove(apparel);
                }

                var apparelsCantWearTogether = SelectedApparels.Where(s => s != apparel && !ApparelUtility.CanWearTogether(s, apparel, SelectedPawn.RaceProps.body));
                foreach (var apparel2 in apparelsCantWearTogether.ToArray())
                {
                    var orRect = itemRect;
                    orRect.xMin += 10f;
                    orRect.center = new Vector2(orRect.center.x, itemRect.yMin - 0.5f);
                    Text.Font = GameFont.Tiny;
                    orRect.ContractedBy(Text.LineHeight / 2);
                    Widgets.Label(orRect, "or");

                    Text.Font = GameFont.Small;
                    itemRect.y -= Text.LineHeight + 4f;
                    Widgets.DrawHighlightIfMouseover(itemRect);
                    Widgets.Label(itemRect, apparel2.label.Truncate(itemRect.width));
                    drawn.Add(apparel2);
                    if(Widgets.ButtonInvisible(itemRect))
                    {
                        SelectedApparels.Remove(apparel2);
                    }
                }
            }
            Widgets.EndScrollView();
            apparelListViewRect.yMax = Math.Max(outerRect.yMax, outerRect.yMax - (itemRect.y - outerRect.yMin));
        }

        public void DoOutfitPreview(Rect rect)
        {

        }

        private bool TinyInfoButton(Rect rect, ThingDef thingDef, ThingDef stuffDef)
        {
            if (InfoCardButtonWorker(rect))
            {
                Find.WindowStack.Add(new Dialog_InfoCard(thingDef, stuffDef, null));
                return true;
            }
            return false;
        }

        private static bool InfoCardButtonWorker(Rect rect)
        {
            MouseoverSounds.DoRegion(rect);
            TooltipHandler.TipRegionByKey(rect, "DefInfoTip");
            bool result = Widgets.ButtonImage(rect, TexButton.Info, GUI.color, true);
            UIHighlighter.HighlightOpportunity(rect, "InfoCard");
            return result;
        }

        private Pawn selPawnInt;

        private string selPawnButtonLabel;

        private QualityCategory selQualityInt = QualityCategory.Normal;

        private string selQualityButtonLabel;

        private ThingDef selStuffInt;

        private List<ThingDef> selStuffList = new List<ThingDef>();

        private string selStuffButtonLabel;

        private Vector2 apparelsScrollPosition;

        private Vector2 listScrollPosition;

        private Rect apparelListViewRect = Rect.zero;

        private List<ApparelLayerDef> selLayersInt = new List<ApparelLayerDef>();

        private ThingDef statsDrawn;

        private ThingDef mouseovered;

        private ThingDef mouseovered2;

        private List<ThingDef> selApparelsInt = new List<ThingDef>();

        private List<BodyPartGroupDef> selBodyPartGroupsInt;

        private List<BodyPartGroupDef> highlightedGroups;

        private Dictionary<Gender, XDocument> svg = new Dictionary<Gender, XDocument>();

        private IEnumerable<(string id, IEnumerable<IEnumerable<Vector2>> polygons)> buttonColliders;

        private Rect rect5;

        private Rect rect6;
    }
}
