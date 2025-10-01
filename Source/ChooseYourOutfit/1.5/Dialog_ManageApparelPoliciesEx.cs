using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using static ChooseYourOutfit.ModCompat;

namespace ChooseYourOutfit;

[StaticConstructorOnStartup]
[HotSwap]
public class Dialog_ManageApparelPoliciesEx : Dialog_ManageApparelPolicies
{
    private Pawn selPawnInt;

    private PawnRenderTree selPawnRenderTree;

    private ApparelPolicy selPolicyInt;

    private Dictionary<string, (BodyPartRecord, List<BodyPartGroupDef>)> existParts = [];

    private string selPawnButtonLabel = "AnyColonist".Translate();

    private QualityCategory selQualityInt = QualityCategory.Normal;

    private string selQualityButtonLabel = QualityCategory.Normal.GetLabel();

    private ThingDef selStuffInt;

    private Dictionary<ThingDef, ThingDef> selStuffDatabase = [];

    private string selStuffButtonLabel;

    private Vector2 layersScrollPosition;

    private Vector2 apparelsScrollPosition;

    private Vector2 listScrollPosition;

    private List<ApparelLayerDef> layerListToShow = [];

    public bool layerListingRequest;

    private ThingDef statsDrawn;

    private ThingDef mouseovered;

    private ThingDef lastMouseovered;

    private ThingDef mouseoveredSelectedApparel;

    private HashSet<ApparelLayerDef> selLayersInt = [];

    private HashSet<ThingDef> selApparelsInt = [];

    private Dictionary<ThingDef, List<ThingDef>> cantWearTogether = [];

    private List<KeyValuePair<bool, ThingDef>> apparelListToShow = [];

    public bool apparelListingRequest;

    private Dictionary<ApparelLayerDef, HashSet<ThingDef>> selectedApparelListToShow = [];

    public bool selectedApparelListingRequest;

    private Dictionary<ApparelLayerDef, bool> collapse = [];

    private List<ThingDef> preApparelsInt = [];

    public List<Apparel> preApparelsApparel = [];

    //private Dictionary<ThingDef, Apparel> apparelDatabase = new Dictionary<ThingDef, Apparel>();

    private IEnumerable<BodyPartGroupDef> selBodyPartGroupsInt;

    private IEnumerable<BodyPartGroupDef> highlightedGroups;

    private Rect svgViewBox;

    private Rect rect5;

    private Rect rect6;

    private Rect rect7;

    private float panelDecrease;

    private Dictionary<string, Vector2[][]> buttonColliders = [];

    public HashSet<ThingDef> allApparels = [];

    private HashSet<ThingDef> canWearAllowed = [];

    private StatsReporter statsReporter;

    //private Dictionary<Apparel, Color> overrideApparelColors = new Dictionary<Apparel, Color>();

    private Dictionary<ThingDef, ThingDef> previewApparelStuff = [];

    public bool inDialogPortraitRequest = false;

    private HashSet<string> bodypartsWhiteList;

    private bool collapseInStorageMenu = true;

    private FloatRange? curFilterHPRange;

    private QualityRange? curFilterQualityRange;

    private Rot4 pawnPreviewRot = Rot4.South;

    private List<ApparelLayerDef> OrderedLayerDefs;

    private ConcurrentDictionary<Action, bool> bodyPartsDrawer = new();

    private static readonly Texture2D ForColonistsTex = ContentFinder<Texture2D>.Get("UI/Commands/ForColonists", true);

    private static Dictionary<Gender, XDocument> SVGs = [];

    private static Dictionary<Gender, Dictionary<string, List<List<Vector2>>>> Colliders = [];

    private static Dictionary<(Gender, string), Texture2D> unfilledParts = [];

    private static Dictionary<(Gender, string), Texture2D> filledParts = [];

    static Dialog_ManageApparelPoliciesEx()
    {
        foreach (Gender gender in Enum.GetValues(typeof(Gender)))
        {
            SVGs[gender] = XDocument.Load(ChooseYourOutfit.content.RootDir + @"/ButtonColliders/" + gender + ".svg");
            Colliders[gender] = SVGInterpreter.SVGToPolygons(SVGs[gender]);

            foreach (var id in Colliders[gender].Keys)
            {
                unfilledParts[(gender, id)] = ContentFinder<Texture2D>.Get($"ChooseYourOutfit/Body/{gender}/Unfilled/{id}", false);
                filledParts[(gender, id)] = ContentFinder<Texture2D>.Get($"ChooseYourOutfit/Body/{gender}/Filled/{id}", false);
            }
        }
    }

    //選択されたポーンを受け取ってOutfit情報だけをDialog_ManageOutfitsのコンストラクタに渡す
    public Dialog_ManageApparelPoliciesEx(Pawn selectedPawn) : base(selectedPawn?.outfits.CurrentApparelPolicy)
    {
        if (ChooseYourOutfit.settings.disableAddedUI) return;

        statsReporter = new StatsReporter(this);
        layersScrollPosition = default;
        apparelsScrollPosition = default;
        listScrollPosition = default;
        selPawnInt = selectedPawn;
        SelectedPolicy = selectedPawn?.outfits.CurrentApparelPolicy;
        selPolicyInt = SelectedPolicy;
        curFilterHPRange = SelectedPolicy?.filter.AllowedHitPointsPercents;
        curFilterQualityRange = SelectedPolicy?.filter.AllowedQualityLevels;
        DefDatabase<ApparelLayerDef>.AllDefsListForReading.ForEach(l => collapse[l] = ChooseYourOutfit.settings.collapseByLayer);
        OrderedLayerDefs = [.. DefDatabase<ApparelLayerDef>.AllDefs.OrderByDescending(l => l.drawOrder)];

        //毎Tickボタンの当たり判定を計算するのは忍びないので先に計算するためボタン周りのrectを先に決めています
        panelDecrease = (1400f - InitialSize.x) / 8f;
        Rect rect = new(Margin + 402f - panelDecrease, Margin + 52f + OffsetHeaderY, 200f - panelDecrease, windowRect.height)
        {
            yMax = InitialSize.y
        };
        rect5 = rect;
        rect5.yMax -= Margin + CloseButSize.y + 13f;
        var infoWidth = 300f - (panelDecrease * 4f);
        rect6 = new Rect(rect5.xMax + 12f, rect5.y, InitialSize.x - rect5.x - rect5.width - infoWidth - 40f - Margin, rect5.height - 15f);
        rect7 = new Rect(InitialSize.x - (Margin * 2f) - infoWidth, rect5.y, infoWidth, rect5.height - 15f);

        if (selectedPawn == null)
        {
            //this.selPawnButtonLabel = "AnyColonist".Translate().ToString();
            //this.buttonColliders = SVGInterpreter.SVGToPolygons(this.svg[Gender.None], this.rect6);
            selPawnInt = Find.CurrentMap.mapPawns.FreeColonists.FirstOrDefault();
            if (selPawnInt == null) selPawnInt = PawnsFinder.AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists.FirstOrDefault();
        }

        foreach (var apparel in DefDatabase<ThingDef>.AllDefs.Where(d => d.IsApparel))
        {
            //this.apparelDatabase.Add(apparel, GetApparel(apparel, SelectedPawn));
            //this.overrideApparelColors.Add(apparelDatabase[apparel], Color.white);
            var defaultStuff = GenStuff.DefaultStuffFor(apparel);
            if (defaultStuff != null)
            {
                previewApparelStuff.Add(apparel, defaultStuff);
            }
            else previewApparelStuff.Add(apparel, null);

            selStuffDatabase.Add(apparel, defaultStuff);
        }

        if (ProstheticNoMissingBodyParts.Active)
        {
            bodypartsWhiteList = [.. ProstheticNoMissingBodyParts.GetWhitelist];
        }

        InitializeByPawn(SelectedPawn);

        if (Current.Game.outfitDatabase.AllOutfits.Any(outfit => outfit == null))
        {
            Log.Error("[ChooseYourOutfit] A Null Apparel Policy has been generated. Please contact the mod author when you get this.");
            Current.Game.outfitDatabase.AllOutfits.RemoveAll(outfit => outfit == null);
        }

        if (SaveStorageSettings.Active && !ChooseYourOutfit.settings.disableAddedUI)
        {
            HarmonyPatches.Instance.Unpatch(SaveStorageSettings.Original, SaveStorageSettings.Postfix);
        }

        if (Outfitted.Active && !ChooseYourOutfit.settings.disableAddedUI)
        {
            HarmonyPatches.Instance.Unpatch(Outfitted.Patch1Original, Outfitted.Patch1);
            HarmonyPatches.Instance.Unpatch(Outfitted.Patch2Original, Outfitted.Patch2);
        }
    }

    // Outfittedパッチでgetterを使いたいのでpublicに公開
    new public ApparelPolicy SelectedPolicy
    {
        get => base.SelectedPolicy;
        protected set => base.SelectedPolicy = value;
    }

    public Pawn SelectedPawn
    {
        get
        {
            return selPawnInt;
        }
    }

    public HashSet<ApparelLayerDef> SelectedLayers
    {
        get
        {
            return selLayersInt;
        }
    }

    public HashSet<ThingDef> SelectedApparels
    {
        get
        {
            return selApparelsInt;
        }
    }

    public List<ThingDef> PreviewedApparels
    {
        get
        {
            return preApparelsInt;
        }
    }

    public IEnumerable<BodyPartGroupDef> SelectedBodypartGroups
    {
        get
        {
            return selBodyPartGroupsInt;
        }
        set
        {
            selBodyPartGroupsInt = value;
        }
    }

    //ManageOutfitsダイアログのウィンドウサイズを変更
    public override Vector2 InitialSize
    {
        get
        {
            return ChooseYourOutfit.settings.disableAddedUI ? base.InitialSize : new Vector2(Math.Min(1400f, UI.screenWidth - 80f), 700f);
        }
    }

    protected override void DoContentsRect(Rect rect)
    {
        if (!ChooseYourOutfit.settings.disableAddedUI)
        {
            rect.width = 200f - panelDecrease;
        }
        base.DoContentsRect(rect);
    }

    public override void DoWindowContents(Rect inRect)
    {
        base.DoWindowContents(inRect);
        if (ChooseYourOutfit.settings.disableAddedUI) return;

        //baseのDoWindowContentsメソッドの後に追加の衣装選択インターフェイスを描画する
        if (SelectedPolicy == null) return;

        if (Input.GetMouseButtonUp(0))
        {
            canWearAllowed.Clear();
            canWearAllowed.AddRange(SelectedPolicy.filter.AllowedThingDefs.Where(a => a != null && a.IsApparel && a.apparel.PawnCanWear(SelectedPawn)));
            if (ChooseYourOutfit.settings.syncFilter && !canWearAllowed.OrderBy(l => l.label).SequenceEqual(SelectedApparels.OrderBy(l => l.label))) LoadFilter();
            if (selPolicyInt != SelectedPolicy)
            {
                layerListingRequest = true;
                selPolicyInt = SelectedPolicy;
                var pawn = SelectedPawn;
                if (SelectedPawn.outfits.CurrentApparelPolicy != selPolicyInt)
                {
                    pawn = PawnsFinder.AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists.FirstOrFallback(p => p.outfits.CurrentApparelPolicy == selPolicyInt, SelectedPawn);
                    if (pawn != SelectedPawn)
                    {
                        InitializeByPawn(pawn);
                    }
                }
            }
            if (curFilterHPRange != SelectedPolicy.filter.AllowedHitPointsPercents)
            {
                curFilterHPRange = SelectedPolicy.filter.AllowedHitPointsPercents;
                layerListingRequest = true;
            }
            if (curFilterQualityRange != SelectedPolicy.filter.AllowedQualityLevels)
            {
                curFilterQualityRange = SelectedPolicy.filter.AllowedQualityLevels;
                layerListingRequest = true;
            }
        }

        if (layerListingRequest)
        {
            ListingLayerToShow();
        }
        if (apparelListingRequest)
        {
            ListingApparelToShow();
        }
        if (selectedApparelListingRequest)
        {
            ListingSelectedApparelToShow();
        }

        layerListingRequest = false;
        apparelListingRequest = false;
        selectedApparelListingRequest = false;

        Task<IEnumerable<Action>>[] tasks = new Task<IEnumerable<Action>>[4];
        //右のインフォカード描画
        if (statsDrawn != lastMouseovered)
        {
            statsDrawn = lastMouseovered;
            statsReporter.Reset(rect7.width - 10f, statsDrawn, selStuffDatabase[statsDrawn], selQualityInt);
        }

        tasks[3] = Task.Run(() => DoInfoCard(rect7));
        //ちらつきを無くすため一番手前に持ってきました

        //apparelLayerのリストを描画
        var layersRect = new Rect(rect5.x, rect5.y + 40f, rect5.width, Math.Min(Text.LineHeight + (Text.LineHeight * layerListToShow.Count), 240f));
        if (layerListToShow.Count == 0)
        {
            Widgets.Label(layersRect, "CYO.NoApparels".Translate());
        }
        else
        {
            tasks[0] = Task.Run(() => DoLayerList(layersRect));
        }

        //apparelのリストを描画
        tasks[1] = Task.Run(() => DoApparelList(new Rect(rect5.x, layersRect.yMax + 12f, rect5.width, rect5.height - layersRect.height - 67f)));

        var scale = rect6.height / svgViewBox.height;
        Rect rect8 = new Rect(rect6.x, rect6.y, rect6.width - (svgViewBox.width * scale) - 10f, rect6.height);

        //選択したapparelのリストを描画
        tasks[2] = Task.Run(() => DoSelectedApparelList(new Rect(rect8.x, rect8.y + rect8.width, rect8.width, rect8.height - rect8.width)));

        //実際のポーンの見た目プレビュー
        DoOutfitPreview(new Rect(rect8.x, rect8.y, rect8.width, rect8.width));

        //ポーンの体を描画するとこ
        //入植者選択ボタン
        Widgets.BeginGroup(rect6);
        var colonistButtonRect = new Rect(0f, 0f, rect8.width - 40f, 35f);
        var gearButtonRect = colonistButtonRect;
        gearButtonRect.x = colonistButtonRect.xMax + 5f;
        gearButtonRect.width = 35f;

        if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(colonistButtonRect, "CYO.Tip.ColonistButton".Translate());
        if (Widgets.ButtonText(colonistButtonRect, selPawnButtonLabel))
        {
            List<FloatMenuOption> options = [.. from opt in GeneratePawnList(SelectedPawn)
                                             select opt.option];
            Find.WindowStack.Add(new FloatMenu(options));
        }

        if (Widgets.ButtonImageWithBG(gearButtonRect, ForColonistsTex, new Vector2(28f, 28f)))
        {
            Find.WindowStack.Add(new Dialog_WornApparelList(this, SelectedPawn, SelectedPolicy));
        }
        DoPawnBodySeparatedByParts(rect6.AtZero()); //ButtonCollidersの基準がViewBoxの位置(0, 0)からなのでここはBeginGroupで合わせています。（代わりに中身はほぼParallel）
        Widgets.EndGroup();

        if (Find.UIRoot.windows.IsOpen<FloatMenu>() && Input.GetMouseButtonDown(0)) Input.ResetInputAxes(); //フロートメニューを閉じる瞬間他のボタンが反応しないようにする

        foreach (var task in tasks)
        {
            if (task == null) continue;
            foreach (var drawer in task.Result) drawer();
        }

        SaveStorageSettings.SaveStorageButtons(this, inRect);
        Outfitted.OutfittedButton(this, inRect);

        if (ChooseYourOutfit.settings.syncFilter)
        {
            ApplyFilter();
        }
    }

    //ドロップダウンメニューのポーンリストを生成
    private IEnumerable<Widgets.DropdownMenuElement<Pawn>> GeneratePawnList(Pawn pawn)
    {
        //yield return new Widgets.DropdownMenuElement<Pawn>
        //{
        //    option = new FloatMenuOption("AnyColonist".Translate(), delegate ()
        //    {
        //        this.SelectedPawn = null;
        //        this.selPawnButtonLabel = "AnyColonist".Translate();
        //        this.buttonColliders = SVGInterpreter.SVGToPolygons(this.svg[Gender.None], this.rect6);
        //    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
        //    payload = pawn
        //};

        foreach (var colonist in Find.Maps.SelectMany(m => m.mapPawns.FreeColonists))
        {
            yield return new Widgets.DropdownMenuElement<Pawn>
            {
                option = new FloatMenuOption(colonist.LabelShortCap, delegate ()
                {
                    InitializeByPawn(colonist);
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
    private IEnumerable<Widgets.DropdownMenuElement<QualityCategory>> GenerateQualityList(QualityCategory quality)
    {
        foreach (var cat in QualityUtility.AllQualityCategories)
        {
            yield return new Widgets.DropdownMenuElement<QualityCategory>
            {
                option = new FloatMenuOption(cat.GetLabel(), () =>
                {
                    selQualityInt = cat;
                    selQualityButtonLabel = cat.GetLabel();
                    if (statsDrawn != null) statsReporter.Reset(290f, statsDrawn, selStuffDatabase[statsDrawn], cat);
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                payload = quality
            };
        }
    }

    //素材リストを生成
    private IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GenerateStuffList(ThingDef tDef)
    {
        foreach (var stuff in GenStuff.AllowedStuffsFor(statsDrawn))
        {
            yield return new Widgets.DropdownMenuElement<ThingDef>
            {
                option = new FloatMenuOption(stuff.LabelAsStuff, delegate ()
                {
                    selStuffDatabase[statsDrawn] = stuff;
                    foreach (var apparel in DefDatabase<ThingDef>.AllDefs.Where(d => d.IsApparel))
                    {
                        if (apparel.stuffCategories?.SequenceEqual(statsDrawn.stuffCategories) ?? false) selStuffDatabase[apparel] = stuff;
                    }
                    selStuffInt = stuff;
                    selStuffButtonLabel = stuff.LabelAsStuff;
                    statsReporter.Reset(290f, statsDrawn, selStuffDatabase[statsDrawn], selQualityInt);

                    if (statsReporter.SortingEntry.entry != null) apparelListingRequest = true;
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                payload = tDef
            };
        }
        yield break;
    }

    private IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GeneratePreviewApparelStuffList(ThingDef apparel)
    {
        foreach (var stuff in GenStuff.AllowedStuffsFor(apparel))
        {
            yield return new Widgets.DropdownMenuElement<ThingDef>
            {
                option = new FloatMenuOption(stuff.LabelAsStuff, delegate ()
                {
                    previewApparelStuff[apparel] = stuff;
                    ChangePreviewedApparels();

                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                payload = apparel
            };
        }
        yield break;
    }

    private IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GenerateContextMenu(ThingDef apparel)
    {
        yield return new Widgets.DropdownMenuElement<ThingDef>
        {
            option = new FloatMenuOption(string.Format("CYO.AddApparelToAllPolicies".Translate(), apparel.label), delegate ()
            {
                Current.Game.outfitDatabase.AllOutfits.ForEach(o => o.filter.SetAllow(apparel, true));
                canWearAllowed.Clear();
                canWearAllowed.AddRange(SelectedPolicy.filter.AllowedThingDefs.Where(a => a != null && a.IsApparel && a.apparel.PawnCanWear(SelectedPawn)));
                LoadFilter();
            }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
            payload = apparel
        };
        yield return new Widgets.DropdownMenuElement<ThingDef>
        {
            option = new FloatMenuOption(string.Format("CYO.RemoveApparelFromAllPolicies".Translate(), apparel.label), delegate ()
            {
                Current.Game.outfitDatabase.AllOutfits.ForEach(o => o.filter.SetAllow(apparel, false));
                canWearAllowed.Clear();
                canWearAllowed.AddRange(SelectedPolicy.filter.AllowedThingDefs.Where(a => a != null && a.IsApparel && a.apparel.PawnCanWear(SelectedPawn)));
                LoadFilter();
            }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
            payload = apparel
        };
        yield break;
    }

    //服のレイヤーリストを描画
    public IEnumerable<Action> DoLayerList(Rect outerRect)
    {
        var viewRect = new Rect(outerRect.x, outerRect.y, outerRect.width, Text.LineHeight + (Text.LineHeight * layerListToShow.Count));
        viewRect.width -= GenUI.ScrollBarWidth + 1f;

        yield return () => Widgets.BeginGroup(outerRect);
        var itemRect = new Rect(0f, 0f, outerRect.width, Text.LineHeight);

        yield return () =>
        {
            Widgets.DrawMenuSection(outerRect.AtZero());
            Widgets.BeginScrollView(outerRect.AtZero(), ref layersScrollPosition, viewRect.AtZero());
            Widgets.Label(new Rect(itemRect.position + new Vector2(20f, 0f), itemRect.size), "CYO.AllLayers".Translate());
            if (Mouse.IsOver(itemRect))
            {
                if (Input.GetMouseButtonUp(0))
                {
                    SelectedLayers.Clear();
                    SelectedLayers.AddRange(OrderedLayerDefs);
                    apparelListingRequest = true;
                    Input.ResetInputAxes();
                }
                Widgets.DrawHighlight(itemRect);
            }
        };

        if (!SelectedLayers.Any(l => layerListToShow.Contains(l)))
        {
            SelectedLayers.Clear();
            SelectedLayers.Add(layerListToShow.Last());
            apparelListingRequest = true;
        }

        foreach (var (layer, i) in layerListToShow.Select((l, i) => (l, i)))
        {
            var curRect = new Rect(itemRect.x, itemRect.y + ((i + 1) * itemRect.height), itemRect.width, itemRect.height);

            yield return () =>
            {
                if (Mouse.IsOver(curRect))
                {
                    if (Input.GetMouseButtonUp(0))
                    {
                        SelectedLayers.Clear();
                        SelectedLayers.Add(layer);
                        apparelListingRequest = true;
                        Input.ResetInputAxes();
                    }
                    Widgets.DrawHighlight(curRect);
                }
            };

            if (SelectedLayers.Contains(layer)) yield return () => Widgets.DrawHighlightSelected(curRect);
            yield return () => Widgets.Label(new Rect(curRect.x + 20f, curRect.y, curRect.width - 40f, curRect.height), layer.label.Truncate(curRect.width - 40f));
        }
        yield return () =>
        {
            Widgets.EndScrollView();
            Widgets.EndGroup();
        };
    }

    //pawnが着られる選択中のレイヤーかつ選択中のボディパーツの服のリストを描画
    public IEnumerable<Action> DoApparelList(Rect outerRect)
    {
        var parentRect = outerRect;

        yield return () =>
        {
            mouseovered = null;
            Widgets.DrawMenuSection(outerRect);
        };

        if (ChooseYourOutfit.settings.syncFilter is false)
        {
            parentRect.height -= 30f;
            var leftButtonRect = new Rect(parentRect.x + 3f, parentRect.yMax + 3f, (parentRect.width / 2) - 4.5f, 24f);
            var rightButtonRect = new Rect(parentRect.x + (parentRect.width / 2) + 1.5f, parentRect.yMax + 3f, (parentRect.width / 2) - 4.5f, 24f);
            yield return () =>
            {
                using (new TextBlock(GameFont.Tiny))
                {
                    if (Widgets.ButtonText(leftButtonRect, "CYO.LoadFilter".Translate()))
                    {
                        LoadFilter();
                    }
                    if (Widgets.ButtonText(rightButtonRect, "CYO.ApplyFilter".Translate()))
                    {
                        ApplyFilter();
                    }
                }
            };
        }

        if (ChooseYourOutfit.settings.showResearchedButton)
        {
            parentRect.height -= Text.LineHeight;
            var filterLabelRect = new Rect(parentRect.x + 3f, parentRect.yMax, parentRect.width - Text.LineHeight - 6f, Text.LineHeight);
            var checkBoxPosition = new Vector2(parentRect.xMax - Text.LineHeight - 3f, parentRect.yMax);
            yield return () =>
            {
                if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(filterLabelRect, "CYO.Tip.Researched".Translate());
                Widgets.Label(filterLabelRect, "CYO.CurrentlyResearched".Translate());
                Widgets.Checkbox(checkBoxPosition, ref ChooseYourOutfit.settings.currentlyResearched, 20f);
                if (Widgets.ButtonInvisible(new Rect(checkBoxPosition, new Vector2(24f, 24f))))
                {
                    layerListingRequest = true;
                }
            };
        }

        if (ChooseYourOutfit.settings.showInStorageButton)
        {
            parentRect.height -= Text.LineHeight;
            var filterLabelRect = new Rect(parentRect.x + 3f, parentRect.yMax, parentRect.width - Text.LineHeight - 6f, Text.LineHeight);
            if (ChooseYourOutfit.settings.currentlyInStorage)
            {
                filterLabelRect.xMin += 15f;
                if (!collapseInStorageMenu)
                {
                    filterLabelRect.y -= Text.LineHeight * 2f;
                    parentRect.height -= Text.LineHeight * 2f;
                    var label1Rect = new Rect(filterLabelRect.x, filterLabelRect.yMax, filterLabelRect.width, Text.LineHeight);
                    var check1Pos = new Vector2(label1Rect.xMax, label1Rect.y);
                    var label2Rect = new Rect(filterLabelRect.x, label1Rect.yMax, filterLabelRect.width, Text.LineHeight);
                    var check2Pos = new Vector2(label2Rect.xMax, label2Rect.y);
                    yield return () =>
                    {
                        if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(label1Rect, "CYO.Tip.ApplyHitPoints".Translate());
                        Widgets.Label(label1Rect, "CYO.ApplyHitPoints".Translate());
                        Widgets.Checkbox(check1Pos, ref ChooseYourOutfit.settings.applyHitPoints, 20f);
                        if (Widgets.ButtonInvisible(new Rect(check1Pos, new Vector2(24f, 24f))))
                        {
                            layerListingRequest = true;
                        }
                        if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(label2Rect, "CYO.Tip.ApplyQuality".Translate());
                        Widgets.Label(label2Rect, "CYO.ApplyQuality".Translate());
                        Widgets.Checkbox(check2Pos, ref ChooseYourOutfit.settings.applyQuality, 20f);
                        if (Widgets.ButtonInvisible(new Rect(check2Pos, new Vector2(24f, 24f))))
                        {
                            layerListingRequest = true;
                        }
                    };
                }
                var tex = collapseInStorageMenu ? TexButton.Reveal : TexButton.Collapse;
                var butRect = new Rect(filterLabelRect.x - 18f, filterLabelRect.y, Text.LineHeight, Text.LineHeight);
                yield return () =>
                {
                    if (Mouse.IsOver(butRect) && Input.GetMouseButtonUp(0))
                    {
                        Input.ResetInputAxes();
                        collapseInStorageMenu = !collapseInStorageMenu;
                    }
                    Widgets.DrawTextureFitted(butRect, tex, 1f);
                };
            }
            var checkBoxPosition = new Vector2(parentRect.xMax - Text.LineHeight - 3f, parentRect.yMax);
            yield return () =>
            {
                if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(filterLabelRect, "CYO.Tip.InStorage".Translate());
                Widgets.Label(filterLabelRect, "CYO.CurrentlyInStorage".Translate());
                Widgets.Checkbox(checkBoxPosition, ref ChooseYourOutfit.settings.currentlyInStorage, 20f);
                if (Widgets.ButtonInvisible(new Rect(checkBoxPosition, new Vector2(24f, 24f))))
                {
                    layerListingRequest = true;
                }
            };
        }

        var outRect = parentRect;
        Rect viewRect = outRect;
        viewRect.height = Text.LineHeight * apparelListToShow?.Count ?? 0f;

        Widgets.AdjustRectsForScrollView(parentRect, ref outRect, ref viewRect);
        Rect itemRect = parentRect;
        itemRect.height = Text.LineHeight;
        Rect iconRect = new Rect(itemRect.x + 15f, itemRect.y, itemRect.height, itemRect.height);
        Rect infoButtonRect = new Rect(itemRect.xMax - itemRect.height - 15f, itemRect.y, itemRect.height, itemRect.height);
        Rect labelRect = new Rect(iconRect.xMax + 5f, itemRect.y, infoButtonRect.xMin - iconRect.xMax - 10f, itemRect.height);
        infoButtonRect = infoButtonRect.ContractedBy(itemRect.height * 0.1f);

        yield return () => Widgets.BeginScrollView(outRect, ref apparelsScrollPosition, viewRect, true);

        //画面に表示されるアパレルの範囲をあらかじめindexとして計算する
        var fromInclusive = (int)Math.Max(apparelsScrollPosition.y / itemRect.height, 0);
        var toExclusive = (int)Math.Min(((apparelsScrollPosition.y + outRect.height) / itemRect.height) + 1, apparelListToShow.Count);

        for (var index = fromInclusive; index < toExclusive; index++)
        {
            var curY = index * itemRect.height;
            //if (curY < this.apparelsScrollPosition.y - itemRect.height || curY > this.apparelsScrollPosition.y + outerRect.height) return;

            var curItemRect = new Rect(itemRect.x, itemRect.y + curY, itemRect.width, itemRect.height);
            var curIconRect = new Rect(iconRect.x, iconRect.y + curY, iconRect.width, iconRect.height);
            var curLabelRect = new Rect(labelRect.x, labelRect.y + curY, labelRect.width, labelRect.height);
            var curInfoButtonRect = new Rect(infoButtonRect.x, infoButtonRect.y + curY, infoButtonRect.width, infoButtonRect.height);

            var apparel = apparelListToShow[index];

            if (!apparel.Key) yield return () => GUI.DrawTexture(curItemRect, SolidColorMaterials.NewSolidColorTexture(new Color(0f, 0f, 0f, 0.3f)));
            if (SelectedApparels.Contains(apparel.Value)) yield return () => Widgets.DrawHighlightSelected(curItemRect);

            yield return () =>
            {
                if (Mouse.IsOver(curItemRect))
                {
                    lastMouseovered = mouseovered = apparel.Value;
                    TooltipHandler.TipRegion(curItemRect, apparel.Value.label + "\n\n" + apparel.Value.DescriptionDetailed);
                    Widgets.DrawHighlight(curItemRect);
                    if (Input.GetMouseButtonUp(0) && !Mouse.IsOver(curInfoButtonRect))
                    {
                        Input.ResetInputAxes();
                        SelectApparel(apparel.Value);
                    }
                    if (Input.GetMouseButtonUp(1))
                    {
                        Input.ResetInputAxes();
                        List<FloatMenuOption> options = [.. from opt in GenerateContextMenu(apparel.Value)
                                                         select opt.option];
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                }
                Widgets.DefIcon(curIconRect, apparel.Value);
                Widgets.Label(curLabelRect, apparel.Value.label.Truncate(labelRect.width));
                TinyInfoButton(curInfoButtonRect, apparel.Value, GenStuff.DefaultStuffFor(apparel.Value));
            };
        }
        yield return () => Widgets.EndScrollView();
    }

    //パーツで分かれたポーンの体を描画
    public void DoPawnBodySeparatedByParts(Rect rect)
    {
        bodyPartsDrawer.Clear();
        var mousePosition = Event.current.mousePosition;
        var isInAnyPolygon = false;
        Parallel.ForEach(existParts, (KeyValuePair<string, (BodyPartRecord part, List<BodyPartGroupDef> groups)> part) =>
        {
            if (buttonColliders[part.Key].Length == 0) Log.Error("[ChooseYourOutfit]Path does not contain any polygons. It may not be closed.");
            float VectorX(Vector2 v) => v.x;
            float VectorY(Vector2 v) => v.y;
            var pos = new Vector2(buttonColliders[part.Key].Min(p => p.Min(VectorX)), buttonColliders[part.Key].Min(p => p.Min(VectorY)));
            var size = new Vector2(buttonColliders[part.Key].Max(p => p.Max(VectorX)), buttonColliders[part.Key].Max(p => p.Max(VectorY))) - pos;

            var isInPolygon = false;

            if (Mouse.IsOver(rect))
            {
                isInPolygon = buttonColliders[part.Key].Any(p => PolygonCollider.IsInPolygon(p, mousePosition));
                if (isInPolygon)
                {
                    isInAnyPolygon = true;
                    highlightedGroups = part.Value.groups;
                    if (Input.GetMouseButtonUp(0))
                    {
                        Input.ResetInputAxes();
                        if (SelectedBodypartGroups != null && part.Value.groups.SequenceEqual(SelectedBodypartGroups))
                        {
                            SelectedBodypartGroups = null;
                            layerListingRequest = true;
                        }
                        else
                        {
                            SelectedBodypartGroups = part.Value.groups;
                            layerListingRequest = true;
                        }
                    }
                }
            }
            var partHasSelGroups = SelectedBodypartGroups?.All(p => part.Value.groups.Contains(p)) ?? false;
            var partHasHlGroups = highlightedGroups?.All(p => part.Value.groups.Contains(p)) ?? false;
            var partHasHlApGroups = mouseovered != null && mouseovered.apparel.bodyPartGroups.Intersect(part.Value.groups).Any();
            var color = partHasSelGroups ? new Color(0.5f, 0.75f, 1f, 1f) : Color.white;

            //このパーツが着ることのできる衣服がある全てのレイヤー
            var allLayersCount = SelectedLayers
                .Count(l => allApparels
                .Where(a => a.apparel.layers.Contains(l))
                .Any(a => part.Value.groups.Any(g => a.apparel.bodyPartGroups.Contains(g))));
            //このパーツが衣服を着ているレイヤー
            var wearLayersCount = SelectedLayers
                .Count(l => SelectedApparels
                .Where(a => a.apparel.layers.Contains(l))
                .Any(a => part.Value.groups.Any(g => a.apparel.bodyPartGroups.Contains(g))));

            var alpha = new Color(1f, 1f, 1f, allLayersCount != 0 ? wearLayersCount / (float)allLayersCount : 0f);

            var unhighlight = !partHasHlGroups ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.white;

            var covered = partHasHlApGroups ? new Color(0.3f, 0.3f, 0.15f, 0.1f) : Color.clear;

            var pawnGender = SelectedPawn.gender;
            var drawGender = pawnGender == Gender.Male || pawnGender == Gender.Female ? pawnGender : Gender.None;
            bodyPartsDrawer.TryAdd(() =>
            {
                var unfilled = unfilledParts[(drawGender, part.Key)];
                if (unfilled != null)
                {
                    GUI.DrawTexture(new Rect(pos, size), unfilled, ScaleMode.ScaleToFit, true, 0f, (color * unhighlight) + covered, 0f, 0f);
                }
                var filled = filledParts[(drawGender, part.Key)];
                if (filled != null)
                {
                    GUI.DrawTexture(new Rect(pos, size), filledParts[(drawGender, part.Key)], ScaleMode.ScaleToFit, true, 0f, (color * alpha * unhighlight) + covered, 0f, 0f);
                }
            }, true);
        });

        if (!isInAnyPolygon)
        {
            highlightedGroups = null;
            var width = svgViewBox.width * rect6.height / svgViewBox.height;
            if (Mouse.IsOver(new Rect(rect.width - width, rect.y, width, rect.height)) && Input.GetMouseButtonUp(0) && SelectedBodypartGroups != null)
            {
                SelectedBodypartGroups = null;
                layerListingRequest = true;
            }
        }
        foreach (var d in bodyPartsDrawer.Keys) d();
    }

    //情報カードを描画
    public IEnumerable<Action> DoInfoCard(Rect rect)
    {
        var rect2 = new Rect(rect.x, rect.y, (rect7.width / 2f) - 2.5f, 35f);

        yield return () =>
        {
            if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(rect2, "CYO.Tip.InfoQuality".Translate());
            if (Widgets.ButtonText(rect2, selQualityButtonLabel))
            {
                List<FloatMenuOption> options = [.. from opt in GenerateQualityList(selQualityInt)
                                                 select opt.option];
                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
        Rect rect4 = new Rect(rect.x, rect.y + 40f, rect.width, rect.height - 40f);
        yield return () => Widgets.DrawMenuSection(rect4);
        if (statsDrawn != null)
        {
            selStuffInt = selStuffDatabase[statsDrawn];

            if (selStuffInt != null)
            {
                selStuffButtonLabel = selStuffInt.LabelAsStuff;

                var rect3 = new Rect(rect2.xMax + 5f, rect.y, rect2.width, 35f);
                yield return () =>
                {
                    if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(rect3, "CYO.Tip.InfoStuff".Translate());
                    if (Widgets.ButtonText(rect3, selStuffButtonLabel))
                    {
                        List<FloatMenuOption> options = [.. from opt in GenerateStuffList(selStuffInt)
                                                         select opt.option];
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };
            }
            Rect rect5 = rect4.ContractedBy(5f);
            yield return () =>
            {
                using (new TextBlock(GameFont.Medium))
                {
                    Widgets.Label(rect5, statsDrawn.label);
                }
            };

            foreach (var draw in statsReporter.DrawStatsWorker(rect5)) yield return draw;
        }
    }

    //選択した服のリストを描画
    public IEnumerable<Action> DoSelectedApparelList(Rect outerRect)
    {
        if (SelectedApparels.Count == 0) yield break;

        Rect rect1 = new Rect(outerRect.x, outerRect.y, outerRect.width - (Text.LineHeight * 2) - 12f - GenUI.ScrollBarWidth + 1f, Text.LineHeight);
        Rect rect2 = new Rect(outerRect.xMax - (Text.LineHeight * 2) - 12f - GenUI.ScrollBarWidth, outerRect.y, (Text.LineHeight * 2) + 12f + GenUI.ScrollBarWidth - 2f, Text.LineHeight);
        yield return () =>
        {
            Widgets.DrawBoxSolidWithOutline(rect1, new Color(0.18f, 0.18f, 0.2f), new Color(0.36f, 0.36f, 0.4f));
            Widgets.Label(new Rect(rect1.x + 3f, rect1.y, rect1.width, rect1.height), "CYO.SelectedApparels".Translate());
            Widgets.DrawBoxSolidWithOutline(rect2, new Color(0.18f, 0.18f, 0.2f), new Color(0.36f, 0.36f, 0.4f));
            Widgets.Label(new Rect(rect2.x + 3f, rect2.y, rect2.width, rect2.height), "CYO.Preview".Translate());
        };

        outerRect.yMin += Text.LineHeight + 1f;

        if (ChooseYourOutfit.settings.showAddBillsButton)
        {
            outerRect.yMax -= 30f;
            var addBillsButtonRect = new Rect(outerRect.x - 6f, outerRect.yMax + 3f, outerRect.width + 12f, 24f);
            yield return () =>
            {
                if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(addBillsButtonRect, "CYO.Tip.AddBills".Translate());
                if (Widgets.ButtonText(addBillsButtonRect, "CYO.AddBills".Translate()))
                {
                    Find.WindowStack.Add(new Dialog_AddBillsConfirm("CYO.AddBillsConfirm.Desc".Translate(), () =>
                    {
                        IEnumerable<ThingDef> apparels;
                        if (Dialog_AddBillsConfirm.restrictToPreviewedApparels) apparels = PreviewedApparels;
                        else apparels = SelectedApparels;
                        Find.WindowStack.Add(new Dialog_AddBillsToWorkTables(apparels, previewApparelStuff));
                    }));
                }
            };
        }

        Rect itemRect = outerRect;
        itemRect.xMax -= GenUI.ScrollBarWidth;
        var viewRect = itemRect;
        itemRect.height = Text.LineHeight;
        viewRect.height = (selectedApparelListToShow.Count + selectedApparelListToShow.Where(l => !collapse[l.Key]).Select(l => l.Value.Count).Sum()) * itemRect.height;
        Rect checkBoxRect = new Rect(itemRect.xMax - itemRect.height, itemRect.y, itemRect.height, itemRect.height);
        Rect stuffRect = new Rect(itemRect.xMax - (itemRect.height * 2), itemRect.y, itemRect.height, itemRect.height);
        var curY = itemRect.y;
        var anyMouseOvered = false;

        yield return () => Widgets.BeginScrollView(outerRect, ref listScrollPosition, viewRect, true);
        foreach (var apparels in selectedApparelListToShow)
        {
            if (apparels.Value.NullOrEmpty()) continue;

            var curLayerY = curY;
            Rect curLayerItemRect = new Rect(itemRect.x, curLayerY, itemRect.width, itemRect.height);
            Rect butRect = new Rect(itemRect.x, curLayerY, itemRect.height, itemRect.height);
            butRect.ContractedBy(3f);
            Texture2D tex = collapse[apparels.Key] ? TexButton.Reveal : TexButton.Collapse;

            yield return () =>
            {
                if (Mouse.IsOver(butRect) && Input.GetMouseButtonUp(0))
                {
                    Input.ResetInputAxes();
                    collapse[apparels.Key] = !collapse[apparels.Key];
                }
                Widgets.DrawTextureFitted(butRect, tex, 1f);
                Widgets.DrawTitleBG(curLayerItemRect);
                Widgets.Label(new Rect(curLayerItemRect.x + curLayerItemRect.height, curLayerItemRect.y, curLayerItemRect.width - curLayerItemRect.height, curLayerItemRect.height), apparels.Key.label);
                Widgets.DrawLineHorizontal(curLayerItemRect.x, curLayerItemRect.y, curLayerItemRect.width);
            };
            curY += itemRect.height;

            if (!collapse[apparels.Key])
            {
                //var fromInclusive = (int)Math.Max((this.listScrollPosition.y - curY + outerRect.height) / itemRect.height - 1, 0);
                //var toExclusive = (int)Math.Min(fromInclusive + outerRect.height / itemRect.height + 4, apparels.list.Count);

                foreach (var (apparel, index) in apparels.Value.Select((a, i) => (a, i)))
                {
                    var curApparelY = curY + (index * itemRect.height);
                    if (curApparelY < listScrollPosition.y + outerRect.height - itemRect.height - (panelDecrease * 4f) || curApparelY > listScrollPosition.y + (outerRect.height * 2f) - (panelDecrease * 4f)) continue;

                    var curItemRect = new Rect(itemRect.x, curApparelY, itemRect.width, itemRect.height);
                    var curCheckBoxRect = new Rect(checkBoxRect.x, curApparelY, checkBoxRect.width, checkBoxRect.height);
                    var curStuffRect = new Rect(stuffRect.x, curApparelY, stuffRect.width, stuffRect.height);

                    var isPreviewed = PreviewedApparels.Contains(apparel);
                    if (mouseoveredSelectedApparel != null)
                    {
                        if (mouseoveredSelectedApparel != apparel && cantWearTogether[mouseoveredSelectedApparel].Contains(apparel))
                            yield return () => Widgets.DrawRectFast(curItemRect, new Color(0.5f, 0f, 0f, 0.15f));
                    }

                    yield return () =>
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
                                    PreviewedApparels.Remove(apparel);
                                    ChangePreviewedApparels();
                                    //this.overrideApparelColors.Remove(apparelDatabase[apparel]);
                                }
                                else
                                {
                                    PreviewedApparels.Add(apparel);
                                    PreviewedApparels.SortBy(a => a.apparel.LastLayer.drawOrder);
                                    PreviewedApparels.RemoveAll(p => p != apparel && cantWearTogether[apparel].Contains(p));
                                    ChangePreviewedApparels();
                                    //this.overrideApparelColors[apparelDatabase[apparel]] = Color.white;

                                }
                            }
                            else if (previewApparelStuff[apparel] != null && Mouse.IsOver(curStuffRect) && Input.GetMouseButtonUp(0)) //ここをDownにするとウィンドウが開いた瞬間閉じる
                            {
                                Input.ResetInputAxes();
                                List<FloatMenuOption> options = [.. from opt in GeneratePreviewApparelStuffList(apparel)
                                                                 select opt.option];
                                Find.WindowStack.Add(new FloatMenu(options));
                                GeneratePreviewApparelStuffList(apparel);
                            }
                            else if (!Mouse.IsOver(curStuffRect) && Input.GetMouseButtonDown(0)) //上の判定がUpのためcurStuffRectの上での判定を除外する必要がある
                            {
                                Input.ResetInputAxes();
                                SelectedApparels.Remove(apparel);
                                PreviewedApparels.Remove(apparel);
                                ChangePreviewedApparels();
                                apparelListingRequest = true;
                                selectedApparelListingRequest = true;
                            }
                        }
                    };

                    yield return () =>
                    {
                        Widgets.Label(curItemRect, apparel.label.Truncate(curItemRect.width - (curItemRect.height * 2)));
                        TooltipHandler.TipRegion(new Rect(curItemRect.x, curItemRect.y, itemRect.width - (itemRect.height * 2), itemRect.height), apparel.label + "\n\n" + apparel.DescriptionDetailed);
                        if (previewApparelStuff[apparel] != null)
                        {
                            Widgets.DefIcon(curStuffRect.ContractedBy(2f), previewApparelStuff[apparel]);
                            if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(curStuffRect, "CYO.Tip.StuffIcon".Translate());
                        }
                        Widgets.CheckboxDraw(curCheckBoxRect.x + 2f, curCheckBoxRect.y + 2f, isPreviewed, !isPreviewed, 20f);
                        if (ChooseYourOutfit.settings.showTooltips) TooltipHandler.TipRegion(curCheckBoxRect, "CYO.Tip.Checkbox".Translate());
                    };

                    //drawer.Enqueue(() => Widgets.DrawLineHorizontal(itemRect.x, curApparelY + itemRect.height, itemRect.width, Color.gray));
                }
                curY += apparels.Value.Count * itemRect.height;
            }
        }
        yield return () => Widgets.EndScrollView();

        if (anyMouseOvered is false) mouseoveredSelectedApparel = null;
    }

    //ポーンの見た目プレビュー
    public void DoOutfitPreview(Rect rect)
    {
        rect = rect.ContractedBy(10f);

        //renderTreeを保存しておく
        var tmpRenderTree = SelectedPawn.Drawer.renderer.renderTree;

        try
        {
            SelectedPawn.Drawer.renderer.renderTree = selPawnRenderTree;
            inDialogPortraitRequest = true;
            GUI.DrawTexture(rect, PortraitsCache.Get(SelectedPawn, rect.size, pawnPreviewRot, new Vector3(0f, 0f, 0.32f), 1f, true, true, true, PreviewedApparels.Count != 0, null, null, false, null));
        }
        finally
        {
            inDialogPortraitRequest = false;
            //renderTreeを返してあげる
            SelectedPawn.Drawer.renderer.renderTree = tmpRenderTree;
            PortraitsCache.Clear();
        }

        var rect1 = new Rect(rect.x, rect.y + 17.5f + (rect.height / 2f) - 12f, 20f, 20f);
        Widgets.DrawTextureFitted(rect1, TexUI.ArrowTexLeft, 0.75f);
        if (Mouse.IsOver(rect1) && Input.GetMouseButtonUp(0))
        {
            Input.ResetInputAxes();
            pawnPreviewRot.Rotate(RotationDirection.Clockwise);
        }

        var rect2 = new Rect(rect.xMax - 20f, rect.y + 17.5f + (rect.height / 2f) - 12f, 20f, 20f);
        Widgets.DrawTextureFitted(rect2, TexUI.ArrowTexRight, 0.75f);
        if (Mouse.IsOver(rect2) && Input.GetMouseButtonUp(0))
        {
            Input.ResetInputAxes();
            pawnPreviewRot.Rotate(RotationDirection.Counterclockwise);
        }
    }

    public void ListingApparelToShow()
    {
        apparelListToShow.Clear();
        var enumerable = (IEnumerable<KeyValuePair<bool, ThingDef>>)allApparels
            .Where(a => SelectedLayers.Intersect(a.apparel.layers).Any())
            .Where(a => a.apparel.bodyPartGroups.Any(g => SelectedBodypartGroups?.Contains(g) ?? true))
            .OrderByDescending(a => a.label)
            .GroupBy(a => SelectedApparels.Any(s => a.Equals(s)) || //その服が選択されていればtrue
            (SelectedApparels.All(s => a == s || !cantWearTogether[a].Contains(s)) && //その服が選択されている全ての服と一緒に着られるならtrue
            ApparelUtility.HasPartsToWear(SelectedPawn, a)))
            .SelectMany(g => g.Select(a => new KeyValuePair<bool, ThingDef>(g.Key, a)))
            .OrderByDescending(a => a.Value.label);

        if (ChooseYourOutfit.settings.currentlyResearched)
        {
            //そのapparelを含むレシピが存在しないか、あるいは研究済みのレシピに含まれているapparelに限定
            var allDefs = DefDatabase<RecipeDef>.AllDefs;
            var availableRecipes = allDefs.Where(r => r.AvailableNow).ToArray();
            enumerable = enumerable.Where(a => allDefs.All(r => r.ProducedThingDef != a.Value) || availableRecipes.Any(r => r.ProducedThingDef == a.Value));
        }

        if (ChooseYourOutfit.settings.currentlyInStorage)
        {
            var allApparels = Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);
            var hpFilter = SelectedPolicy.filter.AllowedHitPointsPercents;
            var quFilter = SelectedPolicy.filter.AllowedQualityLevels;
            enumerable = enumerable.Where(a => allApparels.Any(t =>
                {
                    if (ChooseYourOutfit.settings.applyHitPoints && !hpFilter.Includes(t.HitPoints / (float)t.MaxHitPoints))
                    {
                        return false;
                    }
                    if (ChooseYourOutfit.settings.applyQuality && t.TryGetQuality(out var qc) && !quFilter.Includes(qc))
                    {
                        return false;
                    }
                    return t.def == a.Value && t.IsInAnyStorage();
                }));
        }

        if (ChooseYourOutfit.settings.hideUnregistrable)
        {
            enumerable = enumerable.Where(a => a.Value.IsWithinCategory(ThingCategoryDefOf.Apparel) || a.Value.IsWithinCategory(ThingCategoryDefOf.ApparelArmor)
            || (ModsConfig.IsActive("mlie.findagundamnit") && a.Value.IsWithinCategory(ThingCategoryDefOf.Weapons)));
        }

        if (statsReporter.SelectedEntry != null)
        {
            if (statsReporter.SelectedEntry.category == StatCategoryDefOf.EquippedStatOffsets)
                enumerable = enumerable.Where(a => a.Value.equippedStatOffsets.StatListContains(statsReporter.SelectedEntry.stat));
            else enumerable = enumerable.Where(a => GetValueStringFromSelectedEntry(a.Value) == statsReporter.SelectedEntry.ValueString);
        }

        if (ChooseYourOutfit.settings.apparelListMode) enumerable = enumerable.Where(a => a.Key == true);
        else if (ChooseYourOutfit.settings.moveToBottom) enumerable = enumerable.OrderByDescending(a => a.Key is true);

        if (statsReporter.SortingEntry.entry != null)
        {
            if (statsReporter.SortingEntry.descending) enumerable = enumerable.OrderByDescending(a => GetSortingStatValue(a.Value));
            else enumerable = enumerable.OrderBy(a => GetSortingStatValue(a.Value));
        }
        apparelListToShow.AddRange(enumerable);
    }

    private void ListingSelectedApparelToShow()
    {
        selectedApparelListToShow.Clear();
        foreach (var layer in OrderedLayerDefs)
        {
            if (!selectedApparelListToShow.TryGetValue(layer, out var list) || list == null)
            {
                selectedApparelListToShow[layer] = [];
            }
            selectedApparelListToShow[layer].Clear();
            selectedApparelListToShow[layer].AddRange(SelectedApparels.Where(a => a.apparel.layers.Contains(layer)).OrderByDescending(a => a.label));
        }
    }

    private void ListingLayerToShow()
    {
        bool LayerShouldShow(ApparelLayerDef layer)
        {
            var selectedLayers = SelectedLayers;
            SelectedLayers.Clear();
            SelectedLayers.Add(layer);
            ListingApparelToShow();
            selLayersInt = selectedLayers;
            return apparelListToShow.Count != 0;
        }

        layerListToShow.Clear();
        layerListToShow.AddRange(DefDatabase<ApparelLayerDef>.AllDefs
            .Where(LayerShouldShow)
            .OrderByDescending(l => l.drawOrder));
        ListingApparelToShow();
    }

    public void SelectApparel(ThingDef apparel)
    {
        if (SelectedApparels.Contains(apparel))
        {
            SelectedApparels.Remove(apparel);
            PreviewedApparels.Remove(apparel);
            ChangePreviewedApparels();
            //this.overrideApparelColors.RemoveAll(a => !preApparelsApparel.Contains(a.Key));
            //this.apparelDatabase.RemoveAll(a => a.Key == apparel.Value);
        }
        else
        {
            SelectedApparels.Add(apparel);
            if (!PreviewedApparels.Any(p => apparel != p && !ApparelUtility.CanWearTogether(apparel, p, SelectedPawn.RaceProps.body)))
            {
                PreviewedApparels.Add(apparel);
                PreviewedApparels.SortBy(a => a.apparel.LastLayer.drawOrder);
                ChangePreviewedApparels();
            }
        }
        apparelListingRequest = true;
        selectedApparelListingRequest = true;
    }

    private Apparel GetApparel(ThingDef tDef)
    {
        var apparel = (Apparel)ThingMaker.MakeThing(tDef, previewApparelStuff[tDef]);
        if (previewApparelStuff[tDef] != null && apparel.HasComp<CompColorable>())
        {
            apparel.DrawColor = tDef.GetColorForStuff(previewApparelStuff[tDef]);
        }
        return apparel;
    }

    private void ChangePreviewedApparels()
    {
        preApparelsApparel.ForEach(a => a.Destroy());
        preApparelsApparel.Clear();
        preApparelsApparel.AddRange(PreviewedApparels.Select(p => GetApparel(p))); //drawOrderのためにここは一度リセットして再追加している
        selPawnRenderTree.rootNode = null;
        PortraitsCache.Clear();
    }

    private string GetValueStringFromSelectedEntry(ThingDef apparel)
    {
        var label = statsReporter.SelectedEntry.LabelCap;
        if (label == "Stat_Source_Label".Translate()) return apparel.modContentPack?.Name ?? null;
        if (label == "Covers".Translate()) return apparel.apparel.GetCoveredOuterPartsString(BodyDefOf.Human);
        if (label == "Layer".Translate()) return apparel.apparel.GetLayersString();
        if (label == "Stat_Thing_Apparel_CountsAsClothingNudity_Name".Translate()) return apparel.apparel.countsAsClothingForNudity ? "Yes".Translate() : "No".Translate();
        if (label == "Stat_Thing_Apparel_ValidLifestage".Translate()) return apparel.apparel.developmentalStageFilter.ToCommaList(false).CapitalizeFirst();
        if (label == "Stat_Thing_Apparel_Gender".Translate()) return apparel.apparel.gender.GetLabel(false).CapitalizeFirst();
        IEnumerable<RecipeDef> recipes = from r in DefDatabase<RecipeDef>.AllDefsListForReading
                                         where r.products.Count == 1 && r.products.Any((ThingDefCountClass p) => p.thingDef == apparel) && !r.IsSurgery
                                         select r;
        if (label == "CreatedAt".Translate())
        {
            IEnumerable<string> enumerable = (from u in (from x in recipes
                                                         where x.recipeUsers != null
                                                         select x).SelectMany((RecipeDef r) => r.recipeUsers)
                                              select u.label).Concat(from x in DefDatabase<ThingDef>.AllDefsListForReading
                                                                     where x.recipes != null && x.recipes.Any((RecipeDef y) => y.products.Any((ThingDefCountClass z) => z.thingDef == apparel))
                                                                     select x.label).Distinct<string>();
            return enumerable.ToCommaList(false, false).CapitalizeFirst();
        }
        if (label == "Ingredients".Translate())
        {
            RecipeDef recipeDef = recipes.FirstOrDefault<RecipeDef>();
            List<string> tmpCostList = [];
            if (recipeDef != null && !recipeDef.ingredients.NullOrEmpty<IngredientCount>())
            {
                for (int j = 0; j < recipeDef.ingredients.Count; j++)
                {
                    IngredientCount ingredientCount = recipeDef.ingredients[j];
                    if (!ingredientCount.filter.Summary.NullOrEmpty())
                    {
                        tmpCostList.Add(recipeDef.IngredientValueGetter.BillRequirementsDescription(recipeDef, ingredientCount));
                    }
                }
            }
            return tmpCostList.ToCommaList(false, false);
        }
        return apparel.SpecialDisplayStats(StatRequest.ForEmpty()).FirstOrDefault(s => label == s.LabelCap)?.ValueString ?? null;
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

    private void GetExistPartsAndButtons()
    {
        var hediffSet = SelectedPawn.health.hediffSet;
        bool ExistPart(BodyPartRecord part)
        {
            if (!hediffSet.PartIsMissing(part)) return true;
            if (!ProstheticNoMissingBodyParts.Active) return false;

            //pawnのhediffsのいずれかが対象のパーツの親か親の親のhediffで、かつwhiteListに名前が載ってるならpartsに含める
            return hediffSet.hediffs.Any(h => h != null && bodypartsWhiteList.Contains(h.def.defName) && (h.Part == part?.parent || h.Part == part?.parent?.parent));
        }

        existParts.Clear();
        var parts = SelectedPawn.def.race.body.AllParts.Where(ExistPart);
        foreach (var (id, button) in buttonColliders)
        {
            var folder = SelectedPawn.gender == Gender.Female || SelectedPawn.gender == Gender.Male ? SelectedPawn.gender : Gender.None;

            var part = parts.FirstOrDefault(p => id.EqualsIgnoreCase(p.untranslatedCustomLabel?.Replace(" ", "_")) || id.EqualsIgnoreCase(p.def.defName));
            if (part == null) continue;
            var groups = part.groups.Concat(DefDatabase<BodyPartGroupDef>.AllDefs.Where(g => part.Label.Replace(" ", "").EqualsIgnoreCase(g.defName))).ToList();
            existParts[id] = (part, groups);
        }
    }

    private void LoadFilter()
    {
        SelectedApparels.Clear();
        foreach (var a in canWearAllowed) SelectedApparels.Add(a);

        var addedApparels = canWearAllowed.Where(a => a != null && !SelectedApparels.Contains(a)).Where(a => PreviewedApparels.All(p => !cantWearTogether[a].Contains(p)));
        PreviewedApparels.AddRange(addedApparels);
        PreviewedApparels.SortBy(a => a.apparel.LastLayer.drawOrder);

        selectedApparelListingRequest = true;
        apparelListingRequest = true;
        PreviewedApparels.RemoveAll(a => !SelectedApparels.Contains(a));
        ChangePreviewedApparels();
    }

    private void ApplyFilter()
    {
        foreach (var a in canWearAllowed.OrderBy(a => a.label).Except(SelectedApparels.OrderBy(a => a.label))) SelectedPolicy.filter.SetAllow(a, false);
        foreach (var a in SelectedApparels.OrderBy(a => a.label).Except(canWearAllowed.OrderBy(a => a.label))) SelectedPolicy.filter.SetAllow(a, true);
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
        selPawnInt = pawn;
        selPawnRenderTree = new PawnRenderTree(pawn);
        selPawnButtonLabel = pawn.LabelShortCap;
        allApparels.Clear();
        allApparels.AddRange(DefDatabase<ThingDef>.AllDefs.Where(d => d.IsApparel).Where(a => a.apparel.PawnCanWear(pawn)));
        cantWearTogether.Clear();
        foreach (var apparel in allApparels)
        {
            cantWearTogether.Add(apparel, [.. allApparels.Where(a => !ApparelUtility.CanWearTogether(apparel, a, SelectedPawn.RaceProps.body))]);
        }
        Gender gender;
        if (pawn.gender == Gender.Female || pawn.gender == Gender.Male)
        {
            gender = pawn.gender;
        }
        else
        {
            gender = Gender.None;
        }
        svgViewBox = SVGInterpreter.GetViewBox(SVGs[pawn.gender]);
        float scale = Math.Min(rect6.height / svgViewBox.height, rect6.width / svgViewBox.width);
        Vector2 offset = new Vector2(rect6.width - (svgViewBox.width * scale) - svgViewBox.x,
            (rect6.height / 2f) - (svgViewBox.height / 2f * scale) - svgViewBox.y);

        foreach (var pair in Colliders[pawn.gender])
        {
            buttonColliders[pair.Key] = [.. pair.Value.Select(l => l.Select(v => (v * scale) + offset).ToArray())];
        }

        GetExistPartsAndButtons();

        canWearAllowed.Clear();
        if (SelectedPolicy != null)
        {
            canWearAllowed.AddRange(SelectedPolicy.filter.AllowedThingDefs.Where(a => a != null && a.IsApparel && a.apparel.PawnCanWear(SelectedPawn)));
        }
        LoadFilter();
        layerListingRequest = true;
    }

    private bool InfoCardButtonWorker(Rect rect)
    {
        MouseoverSounds.DoRegion(rect);
        TooltipHandler.TipRegionByKey(rect, "DefInfoTip");
        bool result = Widgets.ButtonImage(rect, TexButton.Info, GUI.color, true);
        UIHighlighter.HighlightOpportunity(rect, "InfoCard");
        return result;
    }

    public override void PostClose()
    {
        base.PostClose();
        ChooseYourOutfit.settings.Write();

        if (SaveStorageSettings.Active && !ChooseYourOutfit.settings.disableAddedUI)
        {
            HarmonyPatches.Instance.Patch(SaveStorageSettings.Original, postfix: SaveStorageSettings.Postfix);
        }
        if (Outfitted.Active && !ChooseYourOutfit.settings.disableAddedUI)
        {
            HarmonyPatches.Instance.Patch(Outfitted.Patch1Original, prefix: Outfitted.Patch1);
            HarmonyPatches.Instance.Patch(Outfitted.Patch2Original, postfix: Outfitted.Patch2);
        }
    }
}
