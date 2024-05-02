using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using HarmonyLib;
using System.Text.RegularExpressions;
using static UnityEngine.Scripting.GarbageCollector;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Net;

namespace ChooseYourOutfit
{
    public class Dialog_ManageApparelPoliciesEx : Dialog_ManageApparelPolicies
    {
        //選択されたポーンを受け取ってOutfit情報だけをDialog_ManageOutfitsのコンストラクタに渡す
        public Dialog_ManageApparelPoliciesEx(Pawn selectedPawn) : base(selectedPawn?.outfits.CurrentApparelPolicy)
        {
            this.statsReporter = new StatsReporter();
            this.apparelsScrollPosition = default;
            this.listScrollPosition = default;
            this.SelectedPawn = selectedPawn;
            DefDatabase<ApparelLayerDef>.AllDefsListForReading.ForEach(l => collapse[l] = ChooseYourOutfit.settings.collapseByLayer);

            this.svg.Add(Gender.None, XDocument.Load(ChooseYourOutfit.content.RootDir + @"/ButtonColliders/" + Gender.None + ".svg"));
            this.svg.Add(Gender.Female, XDocument.Load(ChooseYourOutfit.content.RootDir + @"/ButtonColliders/" + Gender.Female + ".svg"));
            this.svg.Add(Gender.Male, XDocument.Load(ChooseYourOutfit.content.RootDir + @"/ButtonColliders/" + Gender.Male + ".svg"));

            //毎Tickボタンの当たり判定を計算するのは忍びないので先に計算するためボタン周りのrectを先に決めています
            this.rect5 = new Rect(Margin + 400f, Margin + 52f + this.OffsetHeaderY, 200f, this.windowRect.height);
            this.rect5.yMax = this.InitialSize.y;
            this.rect5.yMax -= Margin + Window.CloseButSize.y + 13f;
            this.rect6 = new Rect(rect5.x + this.rect5.width + 10f, this.rect5.y, this.InitialSize.x - rect5.x - rect5.width - 340f - Margin, rect5.height - 15f);

            if (this.SelectedPawn == null)
            {
                //this.selPawnButtonLabel = "AnyColonist".Translate().ToString();
                //this.buttonColliders = SVGInterpreter.SVGToPolygons(this.svg[Gender.None], this.rect6);
                this.SelectedPawn = Find.ColonistBar.Entries.FirstOrDefault().pawn;
            }
            this.selPawnButtonLabel = this.SelectedPawn.LabelShortCap;
            if (this.SelectedPawn.gender == Gender.Female || this.SelectedPawn.gender == Gender.Male)
            {
                this.buttonColliders = SVGInterpreter.SVGToPolygons(this.svg[this.SelectedPawn.gender], this.rect6);
                this.svgViewBox = SVGInterpreter.GetViewBox(this.svg[this.SelectedPawn.gender]);
            }
            else
            {
                this.buttonColliders = SVGInterpreter.SVGToPolygons(this.svg[Gender.None], this.rect6);
                this.svgViewBox = SVGInterpreter.GetViewBox(this.svg[this.SelectedPawn.gender]);
            }

            existParts = GetExistPartsAndButtons(this.buttonColliders);
            this.allApparels = DefDatabase<ThingDef>.AllDefs.Where(d => d.apparel != null).Where(a => a.apparel.PawnCanWear(this.SelectedPawn)).ToHashSet();
            this.layerListToShow = this.ListingLayerToShow();

            foreach (var apparel in allApparels)
            {
                this.cantWearTogether.Add(apparel, allApparels.Where(a => !ApparelUtility.CanWearTogether(apparel, a, SelectedPawn.RaceProps.body)).ToList());
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

            this.canWearAllowed = SelectedPolicy.filter.AllowedThingDefs.Where(a => a.apparel?.PawnCanWear(this.SelectedPawn) ?? false).ToHashSet();
            if (ChooseYourOutfit.settings.syncFilter)
            {
                if (!canWearAllowed.SequenceEqual(SelectedApparels)) loadFilter(canWearAllowed);
            }

            //apparelLayerのリストを描画
            var layersRect = new Rect(rect5.x, rect5.y + 40f, 200f, Text.LineHeight + Text.LineHeight * layerListToShow.Count());
            if (layerListToShow.Count() == 0)
            {
                Widgets.Label(layersRect, "CYO.NoApparels".Translate());
            }
            else
            {
                tasks[0] = (Task.Run(() => this.DoLayerList(layersRect)));
            }

            //apparelのリストを描画
            tasks[1] = (Task.Run(() => this.DoApparelList(new Rect(rect5.x, rect5.y + layersRect.height + 50f, 200f, rect5.height - layersRect.height - 65f))));

            var scale = this.rect6.height / this.svgViewBox.height;
            Rect rect8 = new Rect(this.rect6.x, this.rect6.y, this.rect6.width - this.svgViewBox.width * scale - 10f, this.rect6.height);

            //選択したapparelのリストを描画
            tasks[2] = (Task.Run(() => this.DoSelectedApparelList(new Rect(rect8.x, rect8.y + rect8.width, rect8.width, rect8.height - rect8.width))));

            //実際のポーンの見た目プレビュー
            this.DoOutfitPreview(new Rect(rect8.x, rect8.y, rect8.width, rect8.width));

            //ポーンの体を描画するとこ
            //入植者選択ボタン
            Widgets.BeginGroup(rect6);
            Widgets.Dropdown(new Rect(0f, 0f, 150f, 35f),
                null,
                null,
                (Pawn p) => this.GeneratePawnList(p),
                this.selPawnButtonLabel,
                null,
                null,
                null,
                null,
                true);
            this.DoPawnBodySeparatedByParts(rect6.AtZero()); //ButtonCollidersの基準がViewBoxの位置(0, 0)からなのでここはBeginGroupで合わせています。（代わりに中身はほぼParallel）
            Widgets.EndGroup();

            //右のインフォカード描画
            Rect rect7 = new Rect(inRect.xMax - 300f, rect5.y, 300f, rect5.height - 15f);

            tasks[3] = Task.Run(() => this.DoInfoCard(rect7));

            foreach(var task in tasks)
            {
                if (task == null) continue;
                foreach (var drawer in task.Result) drawer();
            }

            if (ChooseYourOutfit.settings.syncFilter)
            {
                if (!canWearAllowed.SequenceEqual(SelectedApparels)) applyFilter(canWearAllowed);
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
                        this.SelectedPawn = entry.pawn;
                        this.selPawnButtonLabel = this.SelectedPawn.LabelShortCap;
                        this.buttonColliders = SVGInterpreter.SVGToPolygons(this.svg[this.SelectedPawn.gender], this.rect6);
                        foreach (var apparel in allApparels)
                        {
                            cantWearTogether[apparel] = allApparels.Where(a => !ApparelUtility.CanWearTogether(apparel, a, SelectedPawn.RaceProps.body)).ToList();
                        }
                        layerListToShow = ListingLayerToShow();
                        if (this.SelectedPawn.gender == Gender.Female || this.SelectedPawn.gender == Gender.Male)
                        {
                            this.buttonColliders = SVGInterpreter.SVGToPolygons(this.svg[this.SelectedPawn.gender], this.rect6);
                        }
                        else
                        {
                            this.buttonColliders = SVGInterpreter.SVGToPolygons(this.svg[Gender.None], this.rect6);
                        }
                        existParts = GetExistPartsAndButtons(this.buttonColliders);
                        foreach (var apparel in preApparelsApparel)
                        {
                            AccessTools.Field(typeof(ThingOwner), "owner").SetValue(apparel.holdingOwner, this.SelectedPawn.apparel);
                        }
                        this.allApparels = DefDatabase<ThingDef>.AllDefs.Where(d => d.apparel != null).Where(a => a.apparel.PawnCanWear(this.SelectedPawn)).ToHashSet();
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
                        this.selQualityInt = cat;
                        this.selQualityButtonLabel = cat.GetLabel();
                        statsReporter.Reset();
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = quality
                };
            }
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
                        this.selStuffList.Replace(this.selStuffInt, stuff);
                        this.selStuffInt = stuff;
                        this.selStuffButtonLabel = stuff.LabelAsStuff;
                        statsReporter.Reset();
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = tDef
                };
            }
        }
        
        //服のレイヤーリストを描画
        public ConcurrentQueue<Action> DoLayerList(Rect outerRect)
        {
            var drawer = new ConcurrentQueue<Action>();
            var itemRect = new Rect(outerRect.x, outerRect.y, outerRect.width, Text.LineHeight);

            drawer.Enqueue(() =>
            {
                Widgets.DrawMenuSection(outerRect);
                Widgets.Label(new Rect(itemRect.position + new Vector2(20f, 0f), itemRect.size), "CYO.AllLayers".Translate());
                if (Mouse.IsOver(itemRect))
                {
                    if (Input.GetMouseButtonUp(0))
                    {
                        this.SelectedLayers = DefDatabase<ApparelLayerDef>.AllDefs.ToHashSet();
                        this.apparelListToShow = ListingApparelToShow(this.allApparels);
                        Input.ResetInputAxes();
                    }
                    Widgets.DrawHighlight(itemRect);
                }
            });

            if (!this.SelectedLayers.Any(l => layerListToShow.Contains(l)))
            {
                this.SelectedLayers = new HashSet<ApparelLayerDef> { layerListToShow.Last() };
                this.apparelListToShow = ListingApparelToShow(this.allApparels);
            }

            foreach (var (layer, i) in layerListToShow.Select((l, i) => (l, i)))
            {
                var curRect = itemRect;
                curRect.y += itemRect.height * i + itemRect.height; 

                if (Mouse.IsOver(curRect))
                {
                    if (Input.GetMouseButtonUp(0))
                    {
                        this.SelectedLayers = new HashSet<ApparelLayerDef> { layer };
                        this.apparelListToShow = ListingApparelToShow(this.allApparels);
                        Input.ResetInputAxes();
                    }
                    drawer.Enqueue(() => Widgets.DrawHighlight(curRect)); ;
                }
                if (this.SelectedLayers.Contains(layer)) drawer.Enqueue(() => Widgets.DrawHighlightSelected(curRect));
                drawer.Enqueue(() => Widgets.Label(new Rect(curRect.x + 20f, curRect.y, curRect.width - 40f, curRect.height), layer.label.Truncate(curRect.width - 40f)));
            }
            return drawer;
        }

        //pawnが着られる選択中のレイヤーかつ選択中のボディパーツの服のリストを描画
        public ConcurrentQueue<Action> DoApparelList(Rect outerRect)
        {
            var parentRect = outerRect;

            if (ChooseYourOutfit.settings.syncFilter is false) outerRect.height -= 30f;

            var drawer = new ConcurrentQueue<Action>();
            Rect viewRect = outerRect;
            viewRect.height = Text.LineHeight * this.apparelListToShow?.Select(g => g.Count()).Sum() ?? 0f;
            this.mouseovered = null;

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

            Widgets.AdjustRectsForScrollView(parentRect, ref outerRect, ref viewRect);
            Rect itemRect = outerRect;
            itemRect.height = Text.LineHeight;
            Rect iconRect = new Rect(itemRect.x + 15f, itemRect.y, itemRect.height, itemRect.height);
            Rect infoButtonRect = new Rect(itemRect.xMax - itemRect.height - 15f, itemRect.y, itemRect.height, itemRect.height);
            Rect labelRect = new Rect(iconRect.xMax + 5f, itemRect.y, infoButtonRect.xMin - iconRect.xMax + 5f, itemRect.height);
            infoButtonRect = infoButtonRect.ContractedBy(itemRect.height * 0.1f);
            var apparelCountTrue = apparelListToShow?.FirstOrDefault(a => a.Key == true)?.Count() ?? 0f;

            drawer.Enqueue(() => Widgets.BeginScrollView(outerRect, ref this.apparelsScrollPosition, viewRect, true));
            foreach (var group in apparelListToShow)
            {
                Parallel.ForEach(group, apparel =>
                {
                    var curY = group.FirstIndexOf(a => a == apparel) * itemRect.height;
                    if (group.Key == false) curY += apparelCountTrue * itemRect.height;
                    if (curY < this.apparelsScrollPosition.y - itemRect.height || curY > this.apparelsScrollPosition.y + outerRect.height) return;

                    var curItemRect = new Rect(itemRect.x, itemRect.y + curY, itemRect.width, itemRect.height);
                    var curIconRect = new Rect(iconRect.x, iconRect.y + curY, iconRect.width, iconRect.height);
                    var curLabelRect = new Rect(labelRect.x, labelRect.y + curY, labelRect.width, labelRect.height);
                    var curInfoButtonRect = new Rect(infoButtonRect.x, infoButtonRect.y + curY, infoButtonRect.width, infoButtonRect.height);

                    drawer.Enqueue(() =>
                    {
                        if (!group.Key) GUI.DrawTexture(curItemRect, SolidColorMaterials.NewSolidColorTexture(new Color(0f, 0f, 0f, 0.3f)));
                        if (this.SelectedApparels.Contains(apparel)) Widgets.DrawHighlightSelected(curItemRect);

                        if (Mouse.IsOver(curItemRect))
                        {
                            this.lastMouseovered = this.mouseovered = apparel;
                            TooltipHandler.TipRegion(curItemRect, apparel.DescriptionDetailed);
                            Widgets.DrawHighlight(curItemRect);
                            if (Input.GetMouseButtonUp(0))
                            {
                                Input.ResetInputAxes();
                                if (this.SelectedApparels.Contains(apparel))
                                {
                                    var tmp = SelectedApparels.Where(a => a != apparel);
                                    this.SelectedApparels = new ConcurrentBag<ThingDef>();
                                    foreach (var a in tmp) SelectedApparels.Add(a);
                                    this.PreviewedApparels.Remove(apparel);
                                    this.preApparelsApparel.RemoveAll(a => !this.PreviewedApparels.Contains(a.def));
                                    this.apparelListToShow = this.ListingApparelToShow(this.allApparels);
                                    this.selectedApparelListToShow = this.ListingSelectedApparelToShow(this.SelectedApparels);
                                }
                                else
                                {
                                    this.SelectedApparels.Add(apparel); 
                                    if (!this.PreviewedApparels.Any(p => apparel != p && !ApparelUtility.CanWearTogether(apparel, p, this.SelectedPawn.RaceProps.body)))
                                    {
                                        this.PreviewedApparels.Add(apparel);
                                        this.preApparelsApparel.TryAddOrTransfer(this.GetApparel(apparel, this.SelectedPawn));
                                    }
                                    this.apparelListToShow = this.ListingApparelToShow(this.allApparels);
                                    this.selectedApparelListToShow = this.ListingSelectedApparelToShow(this.SelectedApparels);
                                }
                            }
                        }
                        Widgets.DefIcon(curIconRect, apparel);
                        Widgets.Label(curLabelRect, apparel.label);
                        this.TinyInfoButton(curInfoButtonRect, apparel, GenStuff.DefaultStuffFor(apparel));
                    });
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
                    isInPolygon = buttonColliders[part.Key].Any(p => PolygonCollider.IsInPolygon(p, mousePosition));
                    if (isInPolygon)
                    {
                        isInAnyPolygon = true;
                        this.highlightedGroups = part.Value.groups;
                        if (Input.GetMouseButtonUp(0))
                        {
                        this.SelectedBodypartGroups = part.Value.groups;
                        this.apparelListToShow = ListingApparelToShow(this.allApparels);
                        this.layerListToShow = ListingLayerToShow();
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
                    this.apparelListToShow = ListingApparelToShow(this.allApparels);
                    this.layerListToShow = ListingLayerToShow();
                }
            }
            foreach (var d in drawer) d();
        }

        //情報カードを描画
        public ConcurrentQueue<Action> DoInfoCard(Rect rect)
        {
            var drawer = new ConcurrentQueue<Action>();

            drawer.Enqueue(() =>
            {
                Widgets.Dropdown(new Rect(rect.x, rect.y, 145f, 35f),
                    this.selQualityInt,
                    null,
                    (QualityCategory q) => this.GenerateQualityList(q),
                    this.selQualityButtonLabel,
                    null,
                    null,
                    null,
                    null,
                    true);
            });

            if (this.statsDrawn != this.lastMouseovered)
            {
                this.statsDrawn = this.lastMouseovered;
                statsReporter.Reset();
            }

                Rect rect2 = new Rect(rect.x, rect.y + 40f, rect.width, rect.height - 40f);
            drawer.Enqueue(() => Widgets.DrawMenuSection(rect2));
            if (this.statsDrawn != null)
            {
                var selStuffInthisThing = GenStuff.AllowedStuffsFor(this.statsDrawn)?.Intersect(this.selStuffList)?.FirstOrDefault();

                if (GenStuff.AllowedStuffsFor(this.statsDrawn).Count() != 0)
                {
                    if (selStuffInthisThing != null)
                    {
                        this.selStuffInt = selStuffInthisThing;
                        this.selStuffButtonLabel = this.selStuffInt.LabelAsStuff;
                    }
                    else
                    {
                        this.selStuffInt = GenStuff.DefaultStuffFor(this.statsDrawn);
                        this.selStuffList.Add(this.selStuffInt);
                        this.selStuffButtonLabel = this.selStuffInt.LabelAsStuff;
                    }
                    drawer.Enqueue(() =>
                    {
                        Widgets.Dropdown(new Rect(rect.x + 155f, rect.y, 145f, 35f),
                            null,
                            null,
                            (ThingDef s) => this.GenerateStuffList(s),
                            this.selStuffButtonLabel,
                            null,
                            null,
                            null,
                            null,
                            true);
                    });
                }
                drawer.Enqueue(() => statsReporter.DrawStatsReport(rect2.ContractedBy(10f), this.statsDrawn, this.selStuffInt, this.selQualityInt));
            }

            return drawer;
        }
        
        //選択した服のリストを描画
        public ConcurrentQueue<Action> DoSelectedApparelList(Rect outerRect)
        {
            var drawer = new ConcurrentQueue<Action>();
            if (this.SelectedApparels.Count == 0) return drawer;

            this.apparelListViewRect.x = outerRect.x;
            this.apparelListViewRect.width = outerRect.width - GenUI.ScrollBarWidth;
            this.apparelListViewRect.yMin = Math.Min(apparelListViewRect.yMin, outerRect.yMin);
            this.apparelListViewRect.yMax = outerRect.yMax;
            Rect itemRect = this.apparelListViewRect;
            itemRect.y = outerRect.yMax;
            itemRect.height = Text.LineHeight;
            Rect orRect = itemRect;
            orRect.xMin += 10f;
            Rect checkBoxRect = new Rect(itemRect.xMax - itemRect.height, itemRect.y, itemRect.height, itemRect.height);
            checkBoxRect.ContractedBy(2f);
            var curY = itemRect.y;

            drawer.Enqueue(() => Widgets.BeginScrollView(outerRect, ref this.listScrollPosition, this.apparelListViewRect, true));
            foreach (var apparelsInLayer in selectedApparelListToShow)
            {
                curY -= itemRect.height;
                if (!collapse[apparelsInLayer.layer])
                {
                    foreach (var apparels in apparelsInLayer.list)
                    {

                        Parallel.ForEach(apparels, (apparel, state, index) =>
                        {
                            var curApparelY = curY - (itemRect.height + 8f) * index;
                            if (curApparelY - itemRect.height > this.listScrollPosition.y || curApparelY < this.listScrollPosition.y + outerRect.height)
                            {
                                var curItemRect = new Rect(itemRect.x, curApparelY, itemRect.width, itemRect.height);
                                var curOrRect = new Rect(orRect.x, curApparelY - 14f, orRect.width, orRect.height);
                                var curCheckBoxRect = new Rect(checkBoxRect.x, curApparelY + 2f, checkBoxRect.width, checkBoxRect.height);

                                var isPreviewed = this.PreviewedApparels.Contains(apparel);

                                drawer.Enqueue(() =>
                                {
                                    Widgets.Label(curItemRect, apparel.label.Truncate(curItemRect.width - curItemRect.height));
                                    Widgets.CheckboxDraw(curCheckBoxRect.x, curCheckBoxRect.y, isPreviewed, !isPreviewed, 20f);
                                    if (Mouse.IsOver(curItemRect))
                                    {
                                        Widgets.DrawHighlight(curItemRect);
                                        if (Mouse.IsOver(curCheckBoxRect) && Input.GetMouseButtonUp(0))
                                        {
                                            if (isPreviewed)
                                            {
                                                this.PreviewedApparels.Remove(apparel);
                                                this.preApparelsApparel.Clear(); //目的のApparelだけを消してもなんか反映されなかったので一回全消ししてから再追加している
                                                foreach (var p in this.PreviewedApparels) this.preApparelsApparel.TryAddOrTransfer(GetApparel(p, SelectedPawn));
                                            }
                                            else
                                            {
                                                this.PreviewedApparels.Add(apparel);
                                                this.preApparelsApparel.TryAddOrTransfer(GetApparel(apparel, SelectedPawn));
                                                this.PreviewedApparels.RemoveWhere(p => p != apparel && cantWearTogether[apparel].Contains(p));
                                                this.preApparelsApparel.RemoveAll(a => !this.PreviewedApparels.Contains(a.def));
                                            }
                                            Input.ResetInputAxes();
                                        }
                                        else if (Input.GetMouseButtonUp(0))
                                        {
                                            Input.ResetInputAxes();
                                            var tmp = SelectedApparels.Where(a => a != apparel);
                                            this.SelectedApparels = new ConcurrentBag<ThingDef>();
                                            foreach (var a in tmp) SelectedApparels.Add(a);
                                            this.apparelListToShow = ListingApparelToShow(this.allApparels);
                                        }
                                    }
                                });

                                if (index == apparels.Count() - 1) return;

                                drawer.Enqueue(() =>
                                {
                                    using (new TextBlock(GameFont.Tiny)) Widgets.Label(curOrRect, "or");
                                });
                            }
                        });
                        curY -= (itemRect.height + 8f) * apparels.Count() - 8f;
                        var curApparelsY = curY;
                        var color = ChooseYourOutfit.settings.selectedApparelListMode ? Color.white : Color.gray;
                        drawer.Enqueue(() => Widgets.DrawLineHorizontal(itemRect.x, curApparelsY + itemRect.height, itemRect.width, color));
                    }
                }

                var curLayerY = curY;
                Rect curLayerItemRect = new Rect(itemRect.x, curLayerY, itemRect.width, itemRect.height);
                Rect butRect = new Rect(itemRect.x, curLayerY, itemRect.height, itemRect.height);
                butRect.ContractedBy(3f);
                Texture2D tex = collapse[apparelsInLayer.layer] ? TexButton.Reveal : TexButton.Collapse;

                drawer.Enqueue(() =>
                {
                    if (Mouse.IsOver(butRect) && Input.GetMouseButtonUp(0))
                    {
                        Input.ResetInputAxes();
                        collapse[apparelsInLayer.layer] = !collapse[apparelsInLayer.layer];
                    }
                    Widgets.DrawTextureFitted(butRect, tex, 1f);
                    Widgets.DrawTitleBG(curLayerItemRect);
                    Widgets.Label(new Rect(curLayerItemRect.x + curLayerItemRect.height, curLayerItemRect.y, curLayerItemRect.width - curLayerItemRect.height, curLayerItemRect.height), apparelsInLayer.layer.label);
                    Widgets.DrawLineHorizontal(curLayerItemRect.x, curLayerItemRect.y, curLayerItemRect.width);
                });
            }
            drawer.Enqueue(() => Widgets.EndScrollView());

            this.apparelListViewRect.yMin = curY;

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

            bool renderClothes = this.SelectedApparels.Count != 0;

            GUI.DrawTexture(rect, PortraitsCache.Get(this.SelectedPawn, rect.size, Rot4.South, new Vector3(0f, 0f, 0.32f), 1f, true, true, true, renderClothes, null, null, false, null));

            //服とDrawerを返してあげる
            AccessTools.Field(typeof(Pawn_ApparelTracker), "wornApparel").SetValue(this.SelectedPawn.apparel, tmpWornApparel);
            AccessTools.Field(typeof(Pawn), "drawer").SetValue(this.SelectedPawn, tmpDrawer);
        }

        private HashSet<IGrouping<bool, ThingDef>> ListingApparelToShow(IEnumerable<ThingDef> apparels)
        {
            return apparels
                .Where(a => this.SelectedLayers.Any(l => a.apparel.layers.Contains(l)))
                .Where(a => a.apparel.bodyPartGroups.Any(g => this.SelectedBodypartGroups?.Contains(g) ?? true))
                .GroupBy(a => this.SelectedApparels.Any(s => a.Equals(s)) || //その服が選択されていればtrue
                this.SelectedApparels.All(s => a == s || !cantWearTogether[a].Contains(s)) && //その服が選択されている全ての服と一緒に着られるならtrue
                a.apparel.bodyPartGroups.Any(b => this.SelectedPawn.health.hediffSet.GetNotMissingParts().Any(p => p.groups.Contains(b)))) //その服のbodyPartGroupのいずれかをpawnが持っていればtrue
                .OrderByDescending(g => g.Key is true).ToHashSet();
        }

        private IEnumerable<(ApparelLayerDef, IEnumerable<IEnumerable<ThingDef>>)> ListingSelectedApparelToShow(IEnumerable<ThingDef> selectedApparels)
        {
            var listByLayer = selectedApparels.GroupBy(a => a.apparel.LastLayer).OrderBy(g => g.Key.drawOrder);

            foreach (var apparels in listByLayer)
            {
                if (ChooseYourOutfit.settings.selectedApparelListMode)
                {
                    var list = new List<List<ThingDef>>();

                    foreach (var apparel in apparels.ToArray())
                    {
                        var cantWearSelected = cantWearTogether[apparel];
                        cantWearSelected.Remove(apparel);
                        cantWearSelected.Add(apparel);

                        cantWearSelected = cantWearSelected.Where(a => SelectedApparels.Any(s => a == s)).ToList();
                        if (list.All(l => !l.OrderBy(a => a.label).SequenceEqual(cantWearSelected.OrderBy(a => a.label)))) list.Add(cantWearSelected);
                    }
                    yield return (apparels.Key, list);
                }

                else yield return (apparels.Key, apparels.Select(a => new ThingDef[1] { a }));
            }
        }

        private HashSet<ApparelLayerDef> ListingLayerToShow()
        {
            return DefDatabase<ApparelLayerDef>.AllDefs
                .Where(l => allApparels
                .Where(a => a.apparel.bodyPartGroups.Any(g => this.SelectedBodypartGroups?.Contains(g) ?? true)) //選択したbodypartgroupが着られるapparelを持つlayerに限定
                .Any(a => a.apparel.layers.Contains(l)))
                .OrderByDescending(l => l.drawOrder).ToHashSet();
        }

        private Apparel GetApparel(ThingDef tDef, Pawn pawn)
        {
            var apparelThing = tDef.GetConcreteExample(tDef.defaultStuff);
            var thingOwner = new ThingOwner<Thing>(pawn.apparel);
            thingOwner.TryAddOrTransfer(apparelThing);
            apparelThing.holdingOwner = thingOwner;
            var apparelThingWithComps = (ThingWithComps)apparelThing;
            AccessTools.FieldRefAccess<List<ThingComp>>(typeof(ThingWithComps), "comps")(apparelThingWithComps).Add(new CompShield());
            apparelThingWithComps.GetComp<CompShield>().parent = apparelThingWithComps;
            return (Apparel)apparelThingWithComps;
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
                this.preApparelsApparel.TryAddRangeOrTransfer(addedApparels.Select(a => this.GetApparel(a, this.SelectedPawn)));
            }
            this.selectedApparelListToShow = this.ListingSelectedApparelToShow(this.SelectedApparels);
            this.apparelListToShow = this.ListingApparelToShow(this.allApparels);
            this.PreviewedApparels.RemoveWhere(a => !this.SelectedApparels.Contains(a));
            this.preApparelsApparel.RemoveAll(a => !this.PreviewedApparels.Contains(a.def));
        }

        private void applyFilter(IEnumerable<ThingDef> canWearAllowed)
        {
            foreach(var a in canWearAllowed.OrderBy(a => a.label).Except(this.SelectedApparels.OrderBy(a => a.label))) this.SelectedPolicy.filter.SetAllow(a, false);
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

        private List<ThingDef> selStuffList = new List<ThingDef>();

        private string selStuffButtonLabel;

        private Vector2 apparelsScrollPosition;

        private Vector2 listScrollPosition;

        private Rect apparelListViewRect = Rect.zero;

        private HashSet<ApparelLayerDef> selLayersInt = new HashSet<ApparelLayerDef>();

        private HashSet<ApparelLayerDef> layerListToShow;

        private ThingDef statsDrawn;

        private ThingDef mouseovered;

        private ThingDef lastMouseovered;

        private ConcurrentBag<ThingDef> selApparelsInt = new ConcurrentBag<ThingDef>();

        private Dictionary<ThingDef, List<ThingDef>> cantWearTogether = new Dictionary<ThingDef, List<ThingDef>>();

        private HashSet<IGrouping<bool, ThingDef>> apparelListToShow = new HashSet<IGrouping<bool, ThingDef>>();

        private IEnumerable<(ApparelLayerDef layer, IEnumerable<IEnumerable<ThingDef>> list)> selectedApparelListToShow;

        private Dictionary<ApparelLayerDef, bool> collapse = new Dictionary<ApparelLayerDef, bool>();

        private List<ThingDef> preApparelsInt = new List<ThingDef>();

        private ThingOwner<Apparel> preApparelsApparel = new ThingOwner<Apparel>();

        private IEnumerable<BodyPartGroupDef> selBodyPartGroupsInt;

        private IEnumerable<BodyPartGroupDef> highlightedGroups;

        private Dictionary<string, Texture2D> unfilledPart = new Dictionary<string, Texture2D>();

        private Dictionary<string, Texture2D> filledPart = new Dictionary<string, Texture2D>();

        private Dictionary<Gender, XDocument> svg = new Dictionary<Gender, XDocument>();

        private Rect svgViewBox;

        private ConcurrentDictionary<string, IEnumerable<IEnumerable<Vector2>>> buttonColliders;

        private Rect rect5;

        private Rect rect6;

        private HashSet<ThingDef> allApparels;

        private HashSet<ThingDef> canWearAllowed;

        private StatsReporter statsReporter;
    }
}
