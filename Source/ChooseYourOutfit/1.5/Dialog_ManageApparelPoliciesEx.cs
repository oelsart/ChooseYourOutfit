using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using HarmonyLib;
using System.Security.Cryptography;
using static HarmonyLib.Code;
using static UnityEngine.GraphicsBuffer;

namespace ChooseYourOutfit
{
    public class Dialog_ManageApparelPoliciesEx : Dialog_ManageApparelPolicies
    {
        //選択されたポーンを受け取ってOutfit情報だけをDialog_ManageOutfitsのコンストラクタに渡す
        public Dialog_ManageApparelPoliciesEx(Pawn selectedPawn) : base(selectedPawn?.outfits.CurrentApparelPolicy)
        {
            this.statsReporter = new StatsReporter(this);
            this.layersScrollPosition = default;
            this.apparelsScrollPosition = default;
            this.listScrollPosition = default;
            this.SelectedPawn = selectedPawn;
            DefDatabase<ApparelLayerDef>.AllDefsListForReading.ForEach(l => collapse[l] = ChooseYourOutfit.settings.collapseByLayer);

            this.svg.Add(Gender.None, XDocument.Load(ChooseYourOutfit.content.RootDir + @"/ButtonColliders/" + Gender.None + ".svg"));
            this.svg.Add(Gender.Female, XDocument.Load(ChooseYourOutfit.content.RootDir + @"/ButtonColliders/" + Gender.Female + ".svg"));
            this.svg.Add(Gender.Male, XDocument.Load(ChooseYourOutfit.content.RootDir + @"/ButtonColliders/" + Gender.Male + ".svg"));

            //毎Tickボタンの当たり判定を計算するのは忍びないので先に計算するためボタン周りのrectを先に決めています
            this.rect5 = new Rect(Margin + 402f, Margin + 52f + this.OffsetHeaderY, 200f, this.windowRect.height);
            this.rect5.yMax = this.InitialSize.y;
            this.rect5.yMax -= Margin + Window.CloseButSize.y + 13f;
            this.rect6 = new Rect(rect5.xMax + 12f, this.rect5.y, this.InitialSize.x - rect5.x - rect5.width - 340f - Margin, rect5.height - 15f);

            if (selectedPawn == null)
            {
                //this.selPawnButtonLabel = "AnyColonist".Translate().ToString();
                //this.buttonColliders = SVGInterpreter.SVGToPolygons(this.svg[Gender.None], this.rect6);
                this.SelectedPawn = Find.ColonistBar.Entries.FirstOrDefault().pawn;
            }

            this.InitializeByPawn(this.SelectedPawn);

            foreach (var apparel in DefDatabase<ThingDef>.AllDefs.Where(d => d.IsApparel))
            {
                //this.apparelDatabase.Add(apparel, GetApparel(apparel, SelectedPawn));
                //this.overrideApparelColors.Add(apparelDatabase[apparel], Color.white);
                var defaultStuff = GenStuff.DefaultStuffFor(apparel);
                if (defaultStuff != null)
                {
                    defaultStuff.stuffProps.allowColorGenerators = false;
                    this.previewApparelStuff.Add(apparel, defaultStuff);
                }
                else this.previewApparelStuff.Add(apparel, null);

                selStuffDatabase.Add(apparel, defaultStuff);
            }
        }

        private static ThingFilter ApparelGlobalFilter
        {
            get
            {
                if (Dialog_ManageApparelPoliciesEx.apparelGlobalFilter == null)
                {
                    Dialog_ManageApparelPoliciesEx.apparelGlobalFilter = new ThingFilter();
                    Dialog_ManageApparelPoliciesEx.apparelGlobalFilter.SetAllow(ThingCategoryDefOf.Apparel, true, null, null);
                }
                return Dialog_ManageApparelPoliciesEx.apparelGlobalFilter;
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

        private HashSet<ApparelLayerDef> SelectedLayers
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

        private ConcurrentBag<ThingDef> SelectedApparels
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

        private List<ThingDef> PreviewedApparels
        {
            get
            {
                return this.preApparelsInt;
            }
            set
            {
                this.preApparelsInt = value;
            }
        }

        private IEnumerable<BodyPartGroupDef> SelectedBodypartGroups
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
                return new Vector2(ChooseYourOutfit.settings.disableAddedUI ? 700f : 1400f, 700f);
            }
        }

        protected override ApparelPolicy CreateNewPolicy()
        {
            return Current.Game.outfitDatabase.MakeNewOutfit();
        }

        protected override ApparelPolicy GetDefaultPolicy()
        {
            return Current.Game.outfitDatabase.DefaultOutfit();
        }

        protected override AcceptanceReport TryDeletePolicy(ApparelPolicy policy)
        {
            return Current.Game.outfitDatabase.TryDelete(policy);
        }

        protected override List<ApparelPolicy> GetPolicies()
        {
            return Current.Game.outfitDatabase.AllOutfits;
        }


        protected override void DoContentsRect(Rect rect)
        {
            if(!ChooseYourOutfit.settings.disableAddedUI) rect.width = 200f;
            ThingFilterUI.DoThingFilterConfigWindow(rect, this.thingFilterState, base.SelectedPolicy.filter, Dialog_ManageApparelPoliciesEx.ApparelGlobalFilter, 16, null, this.HiddenSpecialThingFilters(), false, false, false, null, null);
        }

        private IEnumerable<SpecialThingFilterDef> HiddenSpecialThingFilters()
        {
            yield return SpecialThingFilterDefOf.AllowNonDeadmansApparel;
            if (ModsConfig.IdeologyActive)
            {
                yield return SpecialThingFilterDefOf.AllowVegetarian;
                yield return SpecialThingFilterDefOf.AllowCarnivore;
                yield return SpecialThingFilterDefOf.AllowCannibal;
                yield return SpecialThingFilterDefOf.AllowInsectMeat;
            }
            yield break;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Task<ConcurrentQueue<Action>>[] tasks = new Task<ConcurrentQueue<Action>>[4];

            if(Current.Game.outfitDatabase.AllOutfits.Any(outfit => outfit == null))
            {
                Log.Error("[ChooseYourOutfit] A Null Apparel Policy has been generated. Please contact the mod author when you get this.");
                AccessTools.Field(typeof(OutfitDatabase), "outfits").SetValue(Current.Game.outfitDatabase, Current.Game.outfitDatabase.AllOutfits.Select((o, i) => o == null ? new ApparelPolicy(i, "Delete This Policy") : o).ToList());
            }

            base.DoWindowContents(inRect);
            if (ChooseYourOutfit.settings.disableAddedUI) return;

            //baseのDoWindowContentsメソッドの後に追加の衣装選択インターフェイスを描画する
            if (SelectedPolicy == null) return;

            layerListingRequest = false;
            apparelListingRequest = false;
            selectedApparelListingRequest = false;

            this.canWearAllowed = SelectedPolicy.filter.AllowedThingDefs.Where(a => a.apparel?.PawnCanWear(this.SelectedPawn) ?? false).ToHashSet();
            if (ChooseYourOutfit.settings.syncFilter)
            {
                if (Input.GetMouseButtonUp(0) && !canWearAllowed.OrderBy(l => l.label).SequenceEqual(SelectedApparels.OrderBy(l => l.label))) loadFilter(canWearAllowed);
            }

            //右のインフォカード描画
            Rect rect7 = new Rect(inRect.xMax - 300f, rect5.y, 300f, rect5.height - 15f);
            
            if (this.statsDrawn != this.lastMouseovered)
            {
                this.statsDrawn = this.lastMouseovered;
                statsReporter.Reset(rect7.width - 10f, this.statsDrawn, this.selStuffInt, this.selQualityInt);
            }

            tasks[3] = Task.Run(() => this.DoInfoCard(rect7));
            //ちらつきを無くすため一番手前に持ってきました

            //apparelLayerのリストを描画
            var layersRect = new Rect(rect5.x, rect5.y + 40f, 200f, Math.Min(Text.LineHeight + Text.LineHeight * layerListToShow.Count(), 240f));
            if (layerListToShow.Count() == 0)
            {
                Widgets.Label(layersRect, "CYO.NoApparels".Translate());
            }
            else
            {
                tasks[0] = (Task.Run(() => this.DoLayerList(layersRect)));
            }

            //apparelのリストを描画
            tasks[1] = (Task.Run(() => this.DoApparelList(new Rect(rect5.x, layersRect.yMax + 12f, 200f, rect5.height - layersRect.height - 67f))));

            var scale = this.rect6.height / this.svgViewBox.height;
            Rect rect8 = new Rect(this.rect6.x, this.rect6.y, this.rect6.width - this.svgViewBox.width * scale - 10f, this.rect6.height);

            //選択したapparelのリストを描画
            tasks[2] = (Task.Run(() => this.DoSelectedApparelList(new Rect(rect8.x, rect8.y + rect8.width, rect8.width, rect8.height - rect8.width))));

            //実際のポーンの見た目プレビュー
            this.DoOutfitPreview(new Rect(rect8.x, rect8.y, rect8.width, rect8.width));

            //ポーンの体を描画するとこ
            //入植者選択ボタン
            Widgets.BeginGroup(rect6);
            var colonistButtonRect = new Rect(0f, 0f, 150f, 35f);
            if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(colonistButtonRect, "CYO.Tip.ColonistButton".Translate());
            if (Widgets.ButtonTextDraggable(colonistButtonRect, this.selPawnButtonLabel) == Widgets.DraggableResult.Pressed)
            {
                List<FloatMenuOption> options = (from opt in GeneratePawnList(this.SelectedPawn)
                                                 select opt.option).ToList<FloatMenuOption>();
                Find.WindowStack.Add(new FloatMenu(options));
            }
            this.DoPawnBodySeparatedByParts(rect6.AtZero()); //ButtonCollidersの基準がViewBoxの位置(0, 0)からなのでここはBeginGroupで合わせています。（代わりに中身はほぼParallel）
            Widgets.EndGroup();

            if (Find.UIRoot.windows.IsOpen<FloatMenu>() && Input.GetMouseButtonDown(0)) Input.ResetInputAxes(); //フロートメニューを閉じる瞬間他のボタンが反応しないようにする

            foreach (var task in tasks)
            {
                if (task == null) continue;
                foreach (var drawer in task.Result) drawer();
            }

            if (this.layerListingRequest) this.layerListToShow = this.ListingLayerToShow();
            if (this.apparelListingRequest) this.apparelListToShow = this.ListingApparelToShow(this.SelectedLayers);
            if (this.selectedApparelListingRequest) this.selectedApparelListToShow = this.ListingSelectedApparelToShow(this.SelectedApparels);

            if (ChooseYourOutfit.settings.syncFilter)
            {
                applyFilter(canWearAllowed);
            }
        }

        //ドロップダウンメニューのポーンリストを生成
        public IEnumerable<Widgets.DropdownMenuElement<Pawn>> GeneratePawnList(Pawn pawn)
        {
            /*yield return new Widgets.DropdownMenuElement<Pawn>
            {
                option = new FloatMenuOption("AnyColonist".Translate(), delegate ()
                {
                    this.SelectedPawn = null;
                    this.selPawnButtonLabel = "AnyColonist".Translate();
                    this.buttonColliders = SVGInterpreter.SVGToPolygons(this.svg[Gender.None], this.rect6);
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                payload = pawn
            };*/

            foreach (var entry in Find.ColonistBar.Entries)
            {
                yield return new Widgets.DropdownMenuElement<Pawn>
                {
                    option = new FloatMenuOption(entry.pawn.LabelShortCap, delegate ()
                    {
                        InitializeByPawn(entry.pawn);

                        foreach (var apparel in PreviewedApparels)
                        {
                            preApparelsApparel.TryAddOrTransfer(GetApparel(apparel));
                        }

                        /*foreach (var apparel in allApparels)
                        {
                            this.overrideApparelColors[apparelDatabase[apparel]] = overrideApparelColors.FirstOrDefault(a => a.Key.def == apparel).Value;
                            this.overrideApparelColors.Remove(overrideApparelColors.FirstOrDefault(a => a.Key.def == apparel).Key);
                            this.apparelDatabase[apparel] = GetApparel(apparel, pawn);
                        }*/
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = pawn
                };
            }
            yield break;
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
                        this.selQualityInt = cat;
                        this.selQualityButtonLabel = cat.GetLabel();
                        if (statsDrawn != null) statsReporter.Reset(290f, statsDrawn, selStuffInt, cat);
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = quality
                };
            }
            yield break;
        }

        //素材リストを生成
        public IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GenerateStuffList(ThingDef tDef)
        {
            foreach (var stuff in GenStuff.AllowedStuffsFor(this.statsDrawn))
            {
                yield return new Widgets.DropdownMenuElement<ThingDef>
                {
                    option = new FloatMenuOption(stuff.LabelAsStuff, delegate ()
                    {
                        this.selStuffDatabase[statsDrawn] = stuff;
                        foreach (var apparel in DefDatabase<ThingDef>.AllDefs.Where(d => d.IsApparel))
                        {
                            if (apparel.stuffCategories?.SequenceEqual(statsDrawn.stuffCategories) ?? false) selStuffDatabase[apparel] = stuff;
                        }
                        this.selStuffInt = stuff;
                        this.selStuffButtonLabel = stuff.LabelAsStuff;
                        statsReporter.Reset(290f, statsDrawn, stuff, selQualityInt);

                        if (statsReporter.SortingEntry.entry != null) this.apparelListingRequest = true;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = tDef
                };
            }
            yield break;
        }

        public IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GeneratePreviewApparelStuffList(ThingDef apparel)
        {
            foreach (var stuff in GenStuff.AllowedStuffsFor(apparel))
            {
                yield return new Widgets.DropdownMenuElement<ThingDef>
                {
                    option = new FloatMenuOption(stuff.LabelAsStuff, delegate ()
                    {
                        this.previewApparelStuff[apparel] = stuff;
                        this.previewApparelStuff[apparel].stuffProps.allowColorGenerators = false;
                        this.preApparelsApparel.Clear();
                        foreach (var p in this.PreviewedApparels) this.preApparelsApparel.TryAddOrTransfer(GetApparel(p));

                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = apparel
                };
            }
            yield break;
        }

        //服のレイヤーリストを描画
        public ConcurrentQueue<Action> DoLayerList(Rect outerRect)
        {
            var drawer = new ConcurrentQueue<Action>();
            var viewRect = new Rect(outerRect.x, outerRect.y, outerRect.width, Text.LineHeight + Text.LineHeight * layerListToShow.Count());
            viewRect.width -= GenUI.ScrollBarWidth + 1f;

            drawer.Enqueue(() => Widgets.BeginGroup(outerRect));
            var itemRect = new Rect(0f, 0f, outerRect.width, Text.LineHeight);

            drawer.Enqueue(() =>
            {
                Widgets.DrawMenuSection(outerRect.AtZero());
                Widgets.BeginScrollView(outerRect.AtZero(), ref layersScrollPosition, viewRect.AtZero());
                Widgets.Label(new Rect(itemRect.position + new Vector2(20f, 0f), itemRect.size), "CYO.AllLayers".Translate());
                if (Mouse.IsOver(itemRect))
                {
                    if (Input.GetMouseButtonUp(0))
                    {
                        this.SelectedLayers = DefDatabase<ApparelLayerDef>.AllDefs.ToHashSet();
                        this.apparelListingRequest = true;
                        Input.ResetInputAxes();
                    }
                    Widgets.DrawHighlight(itemRect);
                }
            });

            if (!this.SelectedLayers.Any(l => layerListToShow.Contains(l)))
            {
                this.SelectedLayers = new HashSet<ApparelLayerDef> { layerListToShow.Last() };
                this.apparelListingRequest = true;
            }

            foreach (var (layer, i) in layerListToShow.Select((l, i) => (l, i)))
            {
                var curRect = new Rect(itemRect.x, itemRect.y + (i + 1) * itemRect.height, itemRect.width, itemRect.height);

                drawer.Enqueue(() =>
                {
                    if (Mouse.IsOver(curRect))
                    {
                        if (Input.GetMouseButtonUp(0))
                        {
                            this.SelectedLayers = new HashSet<ApparelLayerDef> { layer };
                            this.apparelListingRequest = true;
                            Input.ResetInputAxes();
                        }
                        Widgets.DrawHighlight(curRect);
                    }
                });

                if (this.SelectedLayers.Contains(layer)) drawer.Enqueue(() => Widgets.DrawHighlightSelected(curRect));
                drawer.Enqueue(() => Widgets.Label(new Rect(curRect.x + 20f, curRect.y, curRect.width - 40f, curRect.height), layer.label.Truncate(curRect.width - 40f)));
            }
            drawer.Enqueue(() => {
                Widgets.EndScrollView();
                Widgets.EndGroup();
                });
            return drawer;
        }

        //pawnが着られる選択中のレイヤーかつ選択中のボディパーツの服のリストを描画
        public ConcurrentQueue<Action> DoApparelList(Rect outerRect)
        {
            var parentRect = outerRect;

            if (ChooseYourOutfit.settings.syncFilter is false) outerRect.height -= 30f;

            var drawer = new ConcurrentQueue<Action>();
            Rect viewRect = outerRect;
            viewRect.height = Text.LineHeight * this.apparelListToShow?.Count() ?? 0f;

            drawer.Enqueue(() => Widgets.DrawMenuSection(parentRect));

            if (ChooseYourOutfit.settings.syncFilter is false)
            {
                drawer.Enqueue(() =>
                {
                    using (new TextBlock(GameFont.Tiny))
                    {
                        if (Widgets.ButtonText(new Rect(parentRect.x + 3f, parentRect.yMax - 27f, parentRect.width / 2 - 4.5f, 24f), "CYO.LoadFilter".Translate()))
                        {
                            this.loadFilter(canWearAllowed);
                        }
                        if (Widgets.ButtonText(new Rect(parentRect.x + parentRect.width / 2 + 1.5f, parentRect.yMax - 27f, parentRect.width / 2 - 4.5f, 24f), "CYO.ApplyFilter".Translate()))
                        {
                            this.applyFilter(canWearAllowed);
                        }
                    }
                });
            }

            var filterLabelRect = new Rect(outerRect.x + 3f, outerRect.yMax - Text.LineHeight, outerRect.width - Text.LineHeight - 6f, Text.LineHeight);
            var checkBoxPosition = new Vector2(outerRect.xMax - Text.LineHeight - 3f, outerRect.yMax - Text.LineHeight);
            drawer.Enqueue(() =>
            {
                this.mouseovered = null;
                Widgets.Label(filterLabelRect, "CYO.CurrentlyResearched".Translate());
                Widgets.Checkbox(checkBoxPosition, ref filterByCurrentlyResearched, 20f);
                if (Widgets.ButtonInvisible(new Rect(checkBoxPosition, new Vector2(24f, 24f))))
                {
                    this.apparelListingRequest = true;
                    this.layerListingRequest = true;
                }
            });

            outerRect.height -= 24f;

            Widgets.AdjustRectsForScrollView(parentRect, ref outerRect, ref viewRect);
            Rect itemRect = parentRect;
            itemRect.height = Text.LineHeight;
            Rect iconRect = new Rect(itemRect.x + 15f, itemRect.y, itemRect.height, itemRect.height);
            Rect infoButtonRect = new Rect(itemRect.xMax - itemRect.height - 15f, itemRect.y, itemRect.height, itemRect.height);
            Rect labelRect = new Rect(iconRect.xMax + 5f, itemRect.y, infoButtonRect.xMin - iconRect.xMax - 10f, itemRect.height);
            infoButtonRect = infoButtonRect.ContractedBy(itemRect.height * 0.1f);

            drawer.Enqueue(() => Widgets.BeginScrollView(outerRect, ref this.apparelsScrollPosition, viewRect, true));

            //画面に表示されるアパレルの範囲をあらかじめindexとして計算する
            var fromInclusive = (int)Math.Max((this.apparelsScrollPosition.y / itemRect.height), 0);
            var toExclusive = (int)Math.Min((this.apparelsScrollPosition.y + outerRect.height) / itemRect.height + 1, this.apparelListToShow.Count);

            for(var index = fromInclusive; index < toExclusive; index++)
            {
                var curY = index * itemRect.height;
                //if (curY < this.apparelsScrollPosition.y - itemRect.height || curY > this.apparelsScrollPosition.y + outerRect.height) return;

                var curItemRect = new Rect(itemRect.x, itemRect.y + curY, itemRect.width, itemRect.height);
                var curIconRect = new Rect(iconRect.x, iconRect.y + curY, iconRect.width, iconRect.height);
                var curLabelRect = new Rect(labelRect.x, labelRect.y + curY, labelRect.width, labelRect.height);
                var curInfoButtonRect = new Rect(infoButtonRect.x, infoButtonRect.y + curY, infoButtonRect.width, infoButtonRect.height);

                var apparel = apparelListToShow.ElementAt(index);

                if (!apparel.Key) drawer.Enqueue(() => GUI.DrawTexture(curItemRect, SolidColorMaterials.NewSolidColorTexture(new Color(0f, 0f, 0f, 0.3f))));
                if (this.SelectedApparels.Contains(apparel.Value)) drawer.Enqueue(() => Widgets.DrawHighlightSelected(curItemRect));

                drawer.Enqueue(() =>
                {
                    if (Mouse.IsOver(curItemRect))
                    {
                        this.lastMouseovered = this.mouseovered = apparel.Value;
                        TooltipHandler.TipRegion(curItemRect, apparel.Value.label + "\n\n" + apparel.Value.DescriptionDetailed);
                        Widgets.DrawHighlight(curItemRect);
                        if (Input.GetMouseButtonUp(0))
                        {
                            Input.ResetInputAxes();
                            if (this.SelectedApparels.Contains(apparel.Value))
                            {
                                var tmp = SelectedApparels.Where(a => a != apparel.Value);
                                this.SelectedApparels = new ConcurrentBag<ThingDef>();
                                foreach (var a in tmp) SelectedApparels.Add(a);
                                this.PreviewedApparels.Remove(apparel.Value);
                                this.preApparelsApparel.Clear();
                                foreach (var p in this.PreviewedApparels) this.preApparelsApparel.TryAddOrTransfer(GetApparel(p));
                                //this.overrideApparelColors.RemoveAll(a => !preApparelsApparel.Contains(a.Key));
                                //this.apparelDatabase.RemoveAll(a => a.Key == apparel.Value);
                                this.apparelListingRequest = true;
                                this.selectedApparelListingRequest = true;
                            }
                            else
                            {
                                this.SelectedApparels.Add(apparel.Value);
                                if (!this.PreviewedApparels.Any(p => apparel.Value != p && !ApparelUtility.CanWearTogether(apparel.Value, p, this.SelectedPawn.RaceProps.body)))
                                {
                                    this.PreviewedApparels.Add(apparel.Value);
                                    this.PreviewedApparels.SortBy(a => a.apparel.LastLayer.drawOrder);
                                    this.preApparelsApparel.TryAddOrTransfer(GetApparel(apparel.Value));
                                }
                                this.apparelListingRequest = true;
                                this.selectedApparelListingRequest = true;
                            }
                        }
                    }
                    Widgets.DefIcon(curIconRect, apparel.Value);
                    Widgets.Label(curLabelRect, apparel.Value.label.Truncate(labelRect.width));
                    this.TinyInfoButton(curInfoButtonRect, apparel.Value, GenStuff.DefaultStuffFor(apparel.Value));
                });
            }
            drawer.Enqueue(() => Widgets.EndScrollView());
            return drawer;
        }

        //パーツで分かれたポーンの体を描画
        public void DoPawnBodySeparatedByParts(Rect rect)
        {
            var drawer = new ConcurrentQueue<Action>();
            var mousePosition = Event.current.mousePosition;
            var isInAnyPolygon = false;
            Parallel.ForEach(this.existParts, (KeyValuePair<string, (BodyPartRecord part, IEnumerable<BodyPartGroupDef> groups)> part) =>
            {
                if (buttonColliders[part.Key].Count() == 0) Log.Error("[ChooseYourOutfit]Path does not contain any polygons. Path may not be closed.");
                var pos = new Vector2(buttonColliders[part.Key].Min(p => p.Min(v => v.x)), buttonColliders[part.Key].Min(p => p.Min(v => v.y)));
                var size = new Vector2(buttonColliders[part.Key].Max(p => p.Max(v => v.x)), buttonColliders[part.Key].Max(p => p.Max(v => v.y))) - pos;

                var isInPolygon = false;

                if (Mouse.IsOver(rect))
                {
                    isInPolygon = buttonColliders[part.Key].Any(p => polygonCollider.IsInPolygon(p, mousePosition));
                    if (isInPolygon)
                    {
                        isInAnyPolygon = true;
                        this.highlightedGroups = part.Value.groups;
                        if (Input.GetMouseButtonUp(0))
                        {
                            Input.ResetInputAxes();
                            if (SelectedBodypartGroups != null && part.Value.groups.SequenceEqual(SelectedBodypartGroups))
                            {
                                this.SelectedBodypartGroups = null;
                                this.apparelListingRequest = true;
                                this.layerListingRequest = true;
                            }
                            else
                            {
                                this.SelectedBodypartGroups = part.Value.groups;
                                this.apparelListingRequest = true;
                                this.layerListingRequest = true;
                            }
                        }
                    }
                }
                var partHasSelGroups = this.SelectedBodypartGroups?.All(p => part.Value.groups.Contains(p)) ?? false;
                var partHasHlGroups = this.highlightedGroups?.All(p => part.Value.groups.Contains(p)) ?? false;
                var partHasHlApGroups = this.mouseovered == null ? false : this.mouseovered.apparel.bodyPartGroups.Intersect(part.Value.groups).Count() != 0;
                var color = partHasSelGroups ? new Color(0.5f, 0.75f, 1f, 1f) : Color.white;

                //このパーツが着ることのできる衣服がある全てのレイヤー
                var allLayers = this.SelectedLayers
                    .Where(l => allApparels
                    .Where(a => a.apparel.layers.Contains(l))
                    .Any(a => part.Value.groups.Any(g => a.apparel.bodyPartGroups.Contains(g))));
                //このパーツが衣服を着ているレイヤー
                var wearLayers = this.SelectedLayers
                    .Where(l => this.SelectedApparels
                    .Where(a => a.apparel.layers.Contains(l))
                    .Any(a => part.Value.groups.Any(g => a.apparel.bodyPartGroups.Contains(g))));

                var alpha = new Color(1f, 1f, 1f, allLayers.Count() != 0 ? (float)wearLayers.Count() / (float)allLayers.Count() : 0f);

                var unhighlight = !partHasHlGroups ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.white;

                var covered = partHasHlApGroups ? new Color(0.3f, 0.3f, 0.15f, 0.1f) : Color.clear;

                drawer.Enqueue(() =>
                {
                    GUI.DrawTexture(new Rect(pos, size), filledPart[part.Key], ScaleMode.ScaleToFit, true, 0f, color * alpha * unhighlight + covered, 0f, 0f);

                    GUI.DrawTexture(new Rect(pos, size), unfilledPart[part.Key], ScaleMode.ScaleToFit, true, 0f, color * unhighlight + covered, 0f, 0f);
                });
            });

            if (!isInAnyPolygon)
            {
                this.highlightedGroups = null;
                var width = this.svgViewBox.width * this.rect6.height / this.svgViewBox.height;
                if (Mouse.IsOver(new Rect(rect.width - width, rect.y, width, rect.height)) && Input.GetMouseButtonUp(0) && SelectedBodypartGroups != null)
                {
                    this.SelectedBodypartGroups = null;
                    this.apparelListingRequest = true;
                    this.layerListingRequest = true;
                }
            }
            foreach (var d in drawer) d();
        }

        //情報カードを描画
        public ConcurrentQueue<Action> DoInfoCard(Rect rect)
        {
            var drawer = new ConcurrentQueue<Action>();
            var rect2 = new Rect(rect.x, rect.y, 145f, 35f);

            drawer.Enqueue(() =>
            {
                if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(rect2, "CYO.Tip.InfoQuality".Translate());
                if (Widgets.ButtonTextDraggable(rect2, selQualityButtonLabel) == Widgets.DraggableResult.Pressed)
                {
                    List<FloatMenuOption> options = (from opt in GenerateQualityList(selQualityInt)
                                                     select opt.option).ToList<FloatMenuOption>();
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            });
            Rect rect4 = new Rect(rect.x, rect.y + 40f, rect.width, rect.height - 40f);
            drawer.Enqueue(() => Widgets.DrawMenuSection(rect4));
            if (this.statsDrawn != null)
            {
                this.selStuffInt = selStuffDatabase[statsDrawn];

                if (this.statsDrawn.stuffCategories != null)
                {
                    this.selStuffButtonLabel = this.selStuffInt.LabelAsStuff;

                    var rect3 = new Rect(rect.x + 155f, rect.y, 145f, 35f);
                    drawer.Enqueue(() =>
                    {
                        if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(rect3, "CYO.Tip.InfoStuff".Translate());
                        if (Widgets.ButtonTextDraggable(rect3, selStuffButtonLabel) == Widgets.DraggableResult.Pressed)
                        {
                            List<FloatMenuOption> options = (from opt in GenerateStuffList(selStuffInt)
                                                             select opt.option).ToList<FloatMenuOption>();
                            Find.WindowStack.Add(new FloatMenu(options));
                        }
                    });
                }
                Rect rect5 = rect4.ContractedBy(5f);
                drawer.Enqueue(() =>
                {
                    using (new TextBlock(GameFont.Medium))
                    {
                        Widgets.Label(rect5, statsDrawn.label);
                    }
                });

                foreach (var draw in statsReporter.DrawStatsWorker(rect5)) drawer.Enqueue(draw);
            }

            return drawer;
        }
        
        //選択した服のリストを描画
        public ConcurrentQueue<Action> DoSelectedApparelList(Rect outerRect)
        {
            var drawer = new ConcurrentQueue<Action>();
            if (this.SelectedApparels.Count == 0) return drawer;

            Rect rect1 = new Rect(outerRect.x, outerRect.y, outerRect.width - Text.LineHeight * 2 - 12f - GenUI.ScrollBarWidth + 1f, Text.LineHeight);
            Rect rect2 = new Rect(outerRect.xMax - Text.LineHeight * 2 - 12f - GenUI.ScrollBarWidth, outerRect.y, Text.LineHeight * 2 + 12f + GenUI.ScrollBarWidth - 2f, Text.LineHeight);
            drawer.Enqueue(() =>
            {
                Widgets.DrawBoxSolidWithOutline(rect1, new Color(0.18f, 0.18f, 0.2f), new Color(0.36f, 0.36f, 0.4f));
                Widgets.Label(new Rect(rect1.x + 3f, rect1.y, rect1.width, rect1.height), "CYO.SelectedApparels".Translate());
                Widgets.DrawBoxSolidWithOutline(rect2, new Color(0.18f, 0.18f, 0.2f), new Color(0.36f, 0.36f, 0.4f));
                Widgets.Label(new Rect(rect2.x + 3f, rect2.y, rect2.width, rect2.height), "CYO.Preview".Translate());
            });

            outerRect.yMin += Text.LineHeight + 1f;

            if (ChooseYourOutfit.settings.showAddBillsButton)
            {
                outerRect.yMax -= 30f;
                drawer.Enqueue(() =>
                {
                    if (Widgets.ButtonText(new Rect(outerRect.x + 3f, outerRect.yMax + 3f, outerRect.width - 6f, 24f), "CYO.AddBills".Translate()))
                    {
                        Find.WindowStack.Add(new Dialog_AddBillsConfirm("CYO.AddBillsConfirm.Desc".Translate(), () =>
                        {
                            Find.WindowStack.Add(new Dialog_AddBillsToWorkTables(Dialog_AddBillsConfirm.restrictToPreviewedApparels ? this.PreviewedApparels.ToHashSet() : this.SelectedApparels.ToHashSet(), previewApparelStuff));
                        }));
                    }
                });
            }

            Rect itemRect = outerRect;
            itemRect.xMax -= GenUI.ScrollBarWidth;
            var viewRect = itemRect;
            itemRect.height = Text.LineHeight;
            viewRect.height = (selectedApparelListToShow.Count() + selectedApparelListToShow.Where(l => !collapse[l.layer]).Select(l => l.list.Count()).Sum()) * itemRect.height;
            Rect checkBoxRect = new Rect(itemRect.xMax - itemRect.height - 2f, itemRect.y, itemRect.height, itemRect.height);
            checkBoxRect = checkBoxRect.ContractedBy(2f);
            Rect stuffRect = new Rect(itemRect.xMax - itemRect.height * 2 - 2f, itemRect.y, itemRect.height, itemRect.height);
            stuffRect = stuffRect.ContractedBy(2f);
            var curY = itemRect.y;
            var anyMouseOvered = false;

            drawer.Enqueue(() => Widgets.BeginScrollView(outerRect, ref this.listScrollPosition, viewRect, true));
            foreach (var apparelsInLayer in selectedApparelListToShow)
            {
                var apparels = apparelsInLayer;
                var curLayerY = curY;
                Rect curLayerItemRect = new Rect(itemRect.x, curLayerY, itemRect.width, itemRect.height);
                Rect butRect = new Rect(itemRect.x, curLayerY, itemRect.height, itemRect.height);
                butRect.ContractedBy(3f);
                Texture2D tex = collapse[apparels.layer] ? TexButton.Reveal : TexButton.Collapse;

                drawer.Enqueue(() =>
                {
                    if (Mouse.IsOver(butRect) && Input.GetMouseButtonUp(0))
                    {
                        Input.ResetInputAxes();
                        collapse[apparels.layer] = !collapse[apparels.layer];
                    }
                    Widgets.DrawTextureFitted(butRect, tex, 1f);
                    Widgets.DrawTitleBG(curLayerItemRect);
                    Widgets.Label(new Rect(curLayerItemRect.x + curLayerItemRect.height, curLayerItemRect.y, curLayerItemRect.width - curLayerItemRect.height, curLayerItemRect.height), apparels.layer.label);
                    Widgets.DrawLineHorizontal(curLayerItemRect.x, curLayerItemRect.y, curLayerItemRect.width);
                });
                curY += itemRect.height;

                if (!collapse[apparels.layer])
                {
                    var fromInclusive = (int)Math.Max((this.listScrollPosition.y - curY + outerRect.height) / itemRect.height - 1, 0);
                    var toExclusive = (int)Math.Min(fromInclusive + outerRect.height / itemRect.height + 1, apparels.list.Count());

                    for(var index = fromInclusive; index < toExclusive; index++)
                    {
                        var apparel = apparels.list.ElementAt(index);
                        var curApparelY = curY + index * itemRect.height;
                        var curItemRect = new Rect(itemRect.x, curApparelY, itemRect.width, itemRect.height);
                        var curCheckBoxRect = new Rect(checkBoxRect.x + 2f, curApparelY + 2f, checkBoxRect.width, checkBoxRect.height);
                        var curStuffRect = new Rect(stuffRect.x + 2f, curApparelY + 2f, stuffRect.width, stuffRect.height);

                        var isPreviewed = this.PreviewedApparels.Contains(apparel);
                        if (mouseoveredSelectedApparel != null)
                        {
                            if (mouseoveredSelectedApparel != apparel && cantWearTogether[mouseoveredSelectedApparel].Contains(apparel))
                                drawer.Enqueue(() => Widgets.DrawRectFast(curItemRect, new Color(0.5f, 0f, 0f, 0.15f)));
                        }

                        drawer.Enqueue(() =>
                        {
                            if (Mouse.IsOver(curItemRect))
                            {
                                anyMouseOvered = true;
                                mouseoveredSelectedApparel = apparel;
                                Widgets.DrawRectFast(curItemRect, new Color(0.7f, 0.7f, 1f, 0.2f));

                                if (Mouse.IsOver(curCheckBoxRect) && Input.GetMouseButtonDown(0))
                                {
                                    Input.ResetInputAxes();
                                    if (isPreviewed)
                                    {
                                        this.PreviewedApparels.Remove(apparel);
                                        this.preApparelsApparel.Clear(); //目的のApparelだけを消してもなんか反映されなかったので一回全消ししてから再追加している
                                        foreach (var p in this.PreviewedApparels) this.preApparelsApparel.TryAddOrTransfer(GetApparel(p));
                                        //this.overrideApparelColors.Remove(apparelDatabase[apparel]);
                                    }
                                    else
                                    {
                                        this.PreviewedApparels.Add(apparel);
                                        this.PreviewedApparels.SortBy(a => a.apparel.LastLayer.drawOrder);
                                        this.PreviewedApparels.RemoveAll(p => p != apparel && cantWearTogether[apparel].Contains(p));
                                        this.preApparelsApparel.Clear();
                                        foreach (var p in this.PreviewedApparels) this.preApparelsApparel.TryAddOrTransfer(GetApparel(p));
                                        //this.overrideApparelColors[apparelDatabase[apparel]] = Color.white;

                                    }
                                }
                                else if (previewApparelStuff[apparel] != null && Mouse.IsOver(curStuffRect) && Input.GetMouseButtonUp(0)) //ここをDownにするとウィンドウが開いた瞬間閉じる
                                {
                                    Input.ResetInputAxes();
                                    List<FloatMenuOption> options = (from opt in GeneratePreviewApparelStuffList(apparel)
                                                                        select opt.option).ToList<FloatMenuOption>();
                                    Find.WindowStack.Add(new FloatMenu(options));
                                    GeneratePreviewApparelStuffList(apparel);
                                }
                                else if (!Mouse.IsOver(curStuffRect) && Input.GetMouseButtonDown(0)) //上の判定がUpのためcurStuffRectの上での判定を除外する必要がある
                                {
                                    Input.ResetInputAxes();
                                    var tmp = SelectedApparels.Where(a => a != apparel);
                                    this.SelectedApparels = new ConcurrentBag<ThingDef>();
                                    foreach (var a in tmp) SelectedApparels.Add(a);
                                    this.apparelListingRequest = true;
                                    this.PreviewedApparels.Remove(apparel);
                                    this.preApparelsApparel.Clear();
                                    foreach (var p in this.PreviewedApparels) this.preApparelsApparel.TryAddOrTransfer(GetApparel(p));
                                    this.selectedApparelListingRequest = true;
                                }
                            }
                        });

                        drawer.Enqueue(() =>
                        {
                            Widgets.Label(curItemRect, apparel.label.Truncate(curItemRect.width - curItemRect.height * 2));
                            TooltipHandler.TipRegion(new Rect(curItemRect.x, curItemRect.y, itemRect.width - itemRect.height * 2 - 2f, itemRect.height), apparel.label + "\n\n" + apparel.DescriptionDetailed);
                            if (previewApparelStuff[apparel] != null)
                            {
                                Widgets.DefIcon(curStuffRect, previewApparelStuff[apparel]);
                                if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(curCheckBoxRect, "CYO.Tip.StuffIcon".Translate());
                            }
                            Widgets.CheckboxDraw(curCheckBoxRect.x, curCheckBoxRect.y, isPreviewed, !isPreviewed, 20f);
                            if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(curCheckBoxRect, "CYO.Tip.Checkbox".Translate());
                        });

                        //drawer.Enqueue(() => Widgets.DrawLineHorizontal(itemRect.x, curApparelY + itemRect.height, itemRect.width, Color.gray));
                    }
                    curY += apparels.list.Count() * itemRect.height;
                }
            }
            drawer.Enqueue(() => Widgets.EndScrollView());

            if (anyMouseOvered is false) mouseoveredSelectedApparel = null;

            return drawer;
        }

        //ポーンの見た目プレビュー
        public void DoOutfitPreview(Rect rect)
        {
            rect = rect.ContractedBy(10f);

            //現在着ている服とDrawerを保存しておく
            var tmpWornApparel = AccessTools.Field(typeof(Pawn_ApparelTracker), "wornApparel").GetValue(this.SelectedPawn.apparel);
            var tmpDrawer = this.SelectedPawn.Drawer;

            AccessTools.Field(typeof(Pawn), "drawer").SetValue(this.SelectedPawn, new Pawn_DrawTracker(this.SelectedPawn));
            AccessTools.Field(typeof(Pawn_ApparelTracker), "wornApparel").SetValue(this.SelectedPawn.apparel, preApparelsApparel);

            bool renderClothes = this.PreviewedApparels.Count != 0;

            GUI.DrawTexture(rect, PortraitsCache.Get(this.SelectedPawn, rect.size, Rot4.South, new Vector3(0f, 0f, 0.32f), 1f, true, true, true, renderClothes, null, null, false, null));

            //服とDrawerを返してあげる
            AccessTools.Field(typeof(Pawn_ApparelTracker), "wornApparel").SetValue(this.SelectedPawn.apparel, tmpWornApparel);
            AccessTools.Field(typeof(Pawn), "drawer").SetValue(this.SelectedPawn, tmpDrawer);
        }

        public HashSet<KeyValuePair<bool, ThingDef>> ListingApparelToShow(IEnumerable<ApparelLayerDef> layers)
        {
            var list = (IEnumerable<KeyValuePair<bool, ThingDef>>)this.allApparels
                .Where(a => layers.Any(l => a.apparel.layers.Contains(l)))
                .Where(a => a.apparel.bodyPartGroups.Any(g => this.SelectedBodypartGroups?.Contains(g) ?? true))
                .OrderByDescending(a => a.label)
                .GroupBy(a => this.SelectedApparels.Any(s => a.Equals(s)) || //その服が選択されていればtrue
                this.SelectedApparels.All(s => a == s || !cantWearTogether[a].Contains(s)) && //その服が選択されている全ての服と一緒に着られるならtrue
                a.apparel.bodyPartGroups.Any(b => this.SelectedPawn.health.hediffSet.GetNotMissingParts().Any(p => p.groups.Contains(b)))) //その服のbodyPartGroupのいずれかをpawnが持っていればtrue
                .SelectMany(g => g.Select(a => new KeyValuePair<bool, ThingDef>(g.Key, a)))
                .OrderByDescending(a => a.Value.label);

            if (this.filterByCurrentlyResearched)
            {
                //そのapparelを含むレシピが存在しないか、あるいは研究済みのレシピに含まれているapparelに限定
                list = list.Where(a => DefDatabase<RecipeDef>.AllDefs.All(r => r.ProducedThingDef != a.Value) || DefDatabase<RecipeDef>.AllDefs.Where(r => r.AvailableNow).Any(r => r.ProducedThingDef == a.Value));
            }

            if(statsReporter.SelectedEntry != null)
            {
                if (statsReporter.SelectedEntry.LabelCap == "Stat_Source_Label".Translate() ||
                    statsReporter.SelectedEntry.LabelCap == "Stat_Thing_Apparel_CountsAsClothingNudity_Name".Translate() ||
                    statsReporter.SelectedEntry.LabelCap == "Layer".Translate() ||
                    statsReporter.SelectedEntry.LabelCap == "Covers".Translate() ||
                    statsReporter.SelectedEntry.LabelCap == "CreatedAt".Translate() ||
                    statsReporter.SelectedEntry.LabelCap == "Ingredients".Translate() ||
                    statsReporter.SelectedEntry.LabelCap == "Stat_Thing_Apparel_ValidLifestage".Translate())
                    list = list.Where(a => a.Value.SpecialDisplayStats(StatRequest.For(a.Value.GetConcreteExample())).Any(s => s.ValueString == statsReporter.SelectedEntry.ValueString));
                else list = list.Where(a => a.Value.SpecialDisplayStats(StatRequest.For(a.Value.GetConcreteExample())).Any(s => s.LabelCap == statsReporter.SelectedEntry.LabelCap));
            }

            if (ChooseYourOutfit.settings.apparelListMode) list = list.Where(a => a.Key == true);
            else if (ChooseYourOutfit.settings.moveToBottom) list = list.OrderByDescending(a => a.Key is true);

            if (statsReporter.SortingEntry.entry != null)
            {
                if (statsReporter.SortingEntry.descending) list = list.OrderByDescending(a => GetSortingStatValue(a.Value));
                else list = list.OrderBy(a => GetSortingStatValue(a.Value));
            }
            return list.ToHashSet();
        }

        private IEnumerable<(ApparelLayerDef, IEnumerable<ThingDef>)> ListingSelectedApparelToShow(IEnumerable<ThingDef> selectedApparels)
        {
            var lists = new List<(ApparelLayerDef, IEnumerable<ThingDef>)>();
            foreach (var layer in DefDatabase<ApparelLayerDef>.AllDefs.OrderByDescending(l => l.drawOrder))
            {
                var list = SelectedApparels.Where(a => a.apparel.layers.Contains(layer)).OrderByDescending(a => a.label);
                if (list.Count() != 0) lists.Add((layer, list));
            }
            return lists;
        }
            
        private HashSet<ApparelLayerDef> ListingLayerToShow()
        {
            return DefDatabase<ApparelLayerDef>.AllDefs
                .Where(l => ListingApparelToShow(new List<ApparelLayerDef>() { l }).Count() != 0)
                .OrderByDescending(l => l.drawOrder).ToHashSet();
        }

        private Apparel GetApparel(ThingDef tDef)
        {
            var apparelThing = tDef.GetConcreteExample(this.previewApparelStuff[tDef]);
            var apparelThingWithComps = (ThingWithComps)apparelThing;
            var apparel = (Apparel)apparelThingWithComps;
            return apparel;
        }
        private float GetSortingStatValue(ThingDef def)
        {
            if (statsReporter.SortingEntry.entry.category == StatCategoryDefOf.EquippedStatOffsets)
            {
                return def.equippedStatOffsets.GetStatValueFromList(statsReporter.SortingEntry.entry.stat, 0f);
            }
            else
            {
                return def.GetStatValueAbstract(statsReporter.SortingEntry.entry.stat, selStuffDatabase[def]);
            }
        }

        private ConcurrentDictionary<string, (BodyPartRecord part, IEnumerable<BodyPartGroupDef>)> GetExistPartsAndButtons(ConcurrentDictionary<string, IEnumerable<IEnumerable<Vector2>>> buttonColliders)
        {
            var result = new ConcurrentDictionary<string, (BodyPartRecord part, IEnumerable<BodyPartGroupDef> groups)>();
            foreach (var (id, button) in this.buttonColliders)
            {
                var folder = SelectedPawn.gender == Gender.Female || SelectedPawn.gender == Gender.Male ? SelectedPawn.gender : Gender.None;
                this.unfilledPart[id] = ContentFinder<Texture2D>.Get($"ChooseYourOutfit/Body/{folder}/Unfilled/{id}");
                this.filledPart[id] = ContentFinder<Texture2D>.Get($"ChooseYourOutfit/Body/{folder}/Filled/{id}");

                var part = this.SelectedPawn.health.hediffSet.GetNotMissingParts()
                    .FirstOrDefault(p => id.EqualsIgnoreCase(p.untranslatedCustomLabel?.Replace(" ", "_")) || id.EqualsIgnoreCase(p.def.defName));
                if (part == null) continue;
                var groups = part.groups;
                groups.AddRange(DefDatabase<BodyPartGroupDef>.AllDefs.Where(g => part.Label.Replace(" ", "").EqualsIgnoreCase(g.defName)));
                result[id] = (part, groups);
            }
            return result;
        }
        
        private void loadFilter(IEnumerable<ThingDef> canWearAllowed)
        {
            HashSet<ThingDef> addedApparels = canWearAllowed.Where(a => !this.SelectedApparels.Contains(a)).ToHashSet();
            this.SelectedApparels = new ConcurrentBag<ThingDef>();
            foreach (var a in canWearAllowed) SelectedApparels.Add(a);

            if (addedApparels.Count() != 0)
            {
                this.PreviewedApparels.AddRange(addedApparels.Where(a => this.PreviewedApparels.All(p => !cantWearTogether[a].Contains(p))));
                this.PreviewedApparels.SortBy(a => a.apparel.LastLayer.drawOrder);
            }
            this.selectedApparelListingRequest = true;
            this.apparelListingRequest = true;
            this.PreviewedApparels.RemoveAll(a => !this.SelectedApparels.Contains(a));
            this.preApparelsApparel.Clear();
            foreach (var p in this.PreviewedApparels) this.preApparelsApparel.TryAddOrTransfer(GetApparel(p));
        }

        private void applyFilter(IEnumerable<ThingDef> canWearAllowed)
        {
            foreach (var a in canWearAllowed.OrderBy(a => a.label).Except(this.SelectedApparels.OrderBy(a => a.label))) this.SelectedPolicy.filter.SetAllow(a, false);
            foreach (var a in this.SelectedApparels.OrderBy(a => a.label).Except(canWearAllowed.OrderBy(a => a.label))) this.SelectedPolicy.filter.SetAllow(a, true);
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

        private void InitializeByPawn(Pawn pawn)
        {
            this.SelectedPawn = pawn;
            this.selPawnButtonLabel = pawn.LabelShortCap;
            this.allApparels = DefDatabase<ThingDef>.AllDefs.Where(d => d.IsApparel).Where(a => a.apparel.PawnCanWear(pawn)).ToHashSet();
            this.cantWearTogether.Clear();
            foreach (var apparel in allApparels)
            {
                cantWearTogether.Add(apparel, allApparels.Where(a => !ApparelUtility.CanWearTogether(apparel, a, SelectedPawn.RaceProps.body)).ToList());
            }
            this.layerListToShow = ListingLayerToShow();
            if (pawn.gender == Gender.Female || pawn.gender == Gender.Male)
            {
                this.buttonColliders = svgInterpreter.SVGToPolygons(this.svg[pawn.gender], this.rect6);
                this.svgViewBox = svgInterpreter.GetViewBox(this.svg[pawn.gender]);
            }
            else
            {
                this.buttonColliders = svgInterpreter.SVGToPolygons(this.svg[Gender.None], this.rect6);
                this.svgViewBox = svgInterpreter.GetViewBox(this.svg[Gender.None]);
            }
            this.existParts = GetExistPartsAndButtons(this.buttonColliders);

            preApparelsApparel = new ThingOwner<Apparel>(pawn.apparel);
        }

        private static bool InfoCardButtonWorker(Rect rect)
        {
            MouseoverSounds.DoRegion(rect);
            TooltipHandler.TipRegionByKey(rect, "DefInfoTip");
            bool result = Widgets.ButtonImage(rect, TexButton.Info, GUI.color, true);
            UIHighlighter.HighlightOpportunity(rect, "InfoCard");
            return result;
        }

        private readonly ThingFilterUI.UIState thingFilterState = new ThingFilterUI.UIState();

        private static ThingFilter apparelGlobalFilter;

        private Pawn selPawnInt;

        private ConcurrentDictionary<string, (BodyPartRecord, IEnumerable<BodyPartGroupDef>)> existParts;

        private string selPawnButtonLabel = "AnyColonist".Translate();

        private QualityCategory selQualityInt = QualityCategory.Normal;

        private string selQualityButtonLabel = QualityCategory.Normal.GetLabel();

        private ThingDef selStuffInt;

        private Dictionary<ThingDef, ThingDef> selStuffDatabase = new Dictionary<ThingDef, ThingDef>();

        private string selStuffButtonLabel;

        private Vector2 layersScrollPosition;

        private Vector2 apparelsScrollPosition;

        private Vector2 listScrollPosition;

        private HashSet<ApparelLayerDef> selLayersInt = new HashSet<ApparelLayerDef>();

        private HashSet<ApparelLayerDef> layerListToShow;

        public bool layerListingRequest;

        private ThingDef statsDrawn;

        private ThingDef mouseovered;

        private ThingDef lastMouseovered;

        private ThingDef mouseoveredSelectedApparel;

        private ConcurrentBag<ThingDef> selApparelsInt = new ConcurrentBag<ThingDef>();

        private Dictionary<ThingDef, List<ThingDef>> cantWearTogether = new Dictionary<ThingDef, List<ThingDef>>();

        private HashSet<KeyValuePair<bool, ThingDef>> apparelListToShow = new HashSet<KeyValuePair<bool, ThingDef>>();

        public bool apparelListingRequest;

        private IEnumerable<(ApparelLayerDef layer, IEnumerable<ThingDef> list)> selectedApparelListToShow = new List<(ApparelLayerDef layer, IEnumerable<ThingDef> list)>();

        public bool selectedApparelListingRequest;

        private Dictionary<ApparelLayerDef, bool> collapse = new Dictionary<ApparelLayerDef, bool>();

        private List<ThingDef> preApparelsInt = new List<ThingDef>();

        private ThingOwner<Apparel> preApparelsApparel;

        //private Dictionary<ThingDef, Apparel> apparelDatabase = new Dictionary<ThingDef, Apparel>();

        private IEnumerable<BodyPartGroupDef> selBodyPartGroupsInt;

        private IEnumerable<BodyPartGroupDef> highlightedGroups;

        private Dictionary<string, Texture2D> unfilledPart = new Dictionary<string, Texture2D>();

        private Dictionary<string, Texture2D> filledPart = new Dictionary<string, Texture2D>();

        private Dictionary<Gender, XDocument> svg = new Dictionary<Gender, XDocument>();

        private Rect svgViewBox;

        private ConcurrentDictionary<string, IEnumerable<IEnumerable<Vector2>>> buttonColliders;

        private Rect rect5;

        private Rect rect6;

        public HashSet<ThingDef> allApparels;

        private HashSet<ThingDef> canWearAllowed;

        private StatsReporter statsReporter;

        //private Dictionary<Apparel, Color> overrideApparelColors = new Dictionary<Apparel, Color>();

        private Dictionary<ThingDef, ThingDef> previewApparelStuff = new Dictionary<ThingDef, ThingDef>();

        private bool filterByCurrentlyResearched = false;

        private SVGInterpreter svgInterpreter = new SVGInterpreter();

        private PolygonCollider polygonCollider = new PolygonCollider();
    }
}
