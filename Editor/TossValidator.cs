// =================================================================================================
//     Author:			Tomas "SkyToss" Szilagyi
//     Date created:	05.04.2018
// =================================================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TossValidator
{
    public class TossValidator : EditorWindow
    {
        private const string ASSET_PATH_SEPARATOR = "\\";
        private const string UNITY_PACKAGES = "Packages";
        private const string EXPORT_FILE_PATH = "Assets\\UnityValidator_Results.csv";
        private const int MAX_COUNT_DISPLAY_ERRORS = 9999;
        private const int MAX_COUNT_SPECIAL_FOLDERS = 30;
        private const int MAX_COUNT_IGNORE_FOLDERS = 30;
        private const int MAX_COUNT_CONDITION_ROWS = 40;

#region [PRIVATE]

        private static TossValidator _mainWindow;

        private readonly string[] _toolbarTitles =
        {
            "Validator",
            "Rules",
            "Conditions",
            "Settings"
        };

        private static readonly string[] ConditionsTypes =
        {
            "Scene",
            "Prefab",
            "Script",
            "Texture",
            "Graphics3D",
            "Sound",
            "Material",
            "Animation"
        };

        private static DValidatorRules _rules;

        private static readonly List<Error> Errors = new List<Error>();

        /// <summary>
        ///     [0 - ExampleOfNaming]
        ///     [1 - exampleOfNaming]
        ///     [2 - example_Of_Naming]
        ///     [3 - example-Of-Naming]
        ///     [4 - Example_Of_Naming]
        /// </summary>
        private static readonly string[] NamingPatterns =
        {
            "ExampleOfNaming",
            "exampleOfNaming",
            "example_of_naming",
            "example-of-naming",
            "Example_Of_Naming"
        };

        private static Icon _icon;

        private static Filter _filter = new Filter
        {
            controlSpecialFolders = true,
            controlFolders = true,
            controlPrefabs = true,
            controlScripts = true,
            controlTextures = true,
            controlScenes = true,
            controlGraphics3D = true,
            controlSounds = true,
            controlMaterials = true,
            controlAnimations = true
        };

        private static Settings _settings = new Settings
        {
            countDisplayErrors = 999,
            countSpecialFolders = 15,
            countIgnoreFolders = 15,
            countConditionRows = 15
        };

        private static GuiValue _guiValue = new GuiValue
        {
            foldoutPatterns = true,
            titlePatterns = "PATTERNS OF NAMING",
            foldoutRootFolders = true,
            titleRootFolders = "ROOT FOLDERS",
            foldoutSpecialFolders = true,
            titleSpecialFolders = "SPECIAL FOLDERS",
            foldoutIgnoreFolders = true,
            titleIgnoreFolders = "IGNORE FOLDERS",

            labelSpecialFolder = GetFormattedLabel("Path: Assets\\"),
            labelIgnoreFolder = GetFormattedLabel("Folder name:"),

            addButtonColor = new Color(0.5f, 0.8f, 1.0f),
            deleteButtonColor = new Color(1.0f, 0.4f, 0.4f),
            duplicateButtonColor = new Color(1.0f, 1.0f, 0.2f),

            styleEvenRow = new GUIStyle(),
            styleOddRow = new GUIStyle(),
            styleDynamicMarginLeft = new GUIStyle(),
            styleExportButtonMarginLeft = new GUIStyle()
        };

        private static int _countErrors;
        private static long _controlTime;

#endregion

        [InitializeOnLoadMethod]
        public static void OnProjectLoadedInEditor()
        {
            //ConsoleDebugLog("[TOSS VALIDATOR] --> OnProjectLoadedInEditor()");
            InitializeValidator();
        }

        private void OnGUI()
        {
            _guiValue.editorActualWidth = EditorGUIUtility.currentViewWidth;

            _guiValue.indexToolbarSelected = GUI.Toolbar(
                new Rect((_guiValue.editorActualWidth - 700) / 2, 10, 700, 30),
                _guiValue.indexToolbarSelected,
                _toolbarTitles);

            switch (_guiValue.indexToolbarSelected)
            {
                case 0:
                    DrawValidator(_guiValue.editorActualWidth);
                    break;
                case 1:
                    DrawRules();
                    break;
                case 2:
                    DrawConditions();
                    break;
                case 3:
                    DrawSettings(_guiValue.editorActualWidth);
                    break;
                default:
                    DrawValidator(_guiValue.editorActualWidth);
                    break;
            }
        }

        private static void ControlProject()
        {
            //ConsoleDebugLog("[TOSS VALIDATOR] --> ControlProject()");

            InitializeValidator();

            var executeTimer = new Stopwatch();

            executeTimer.Start();
            //--- START CONTROL --------------------------------------------------------------------

            ResetErrorData();

            if (_filter.controlSpecialFolders && IsPossibilityDisplayErrors())
            {
                ControlSpecialFolders();
            }

            if (_filter.controlFolders && IsPossibilityDisplayErrors())
            {
                ControlFolders();
            }

            if (_filter.controlPrefabs && IsPossibilityDisplayErrors())
            {
                ControlPrefabs();
            }

            if (_filter.controlScripts && IsPossibilityDisplayErrors())
            {
                ControlScripts();
            }

            if (_filter.controlTextures && IsPossibilityDisplayErrors())
            {
                ControlTextures();
            }

            if (_filter.controlScenes && IsPossibilityDisplayErrors())
            {
                ControlScenes();
            }

            if (_filter.controlGraphics3D && IsPossibilityDisplayErrors())
            {
                ControlModels();
            }

            if (_filter.controlSounds && IsPossibilityDisplayErrors())
            {
                ControlSounds();
            }

            if (_filter.controlMaterials && IsPossibilityDisplayErrors())
            {
                ControlMaterials();
            }

            if (_filter.controlAnimations && IsPossibilityDisplayErrors())
            {
                ControlAnimations();
            }

            //--- STOP CONTROL ---------------------------------------------------------------------
            executeTimer.Stop();
            _controlTime = executeTimer.ElapsedMilliseconds;
        }

        private void OnDestroy() => SaveValidatorStates();

#region [CONTROL METHODS]

        private static void ControlSpecialFolders()
        {
            InitializeValidator();

            if (_rules == null) return;

            var specialFolders = _rules._specialFolders;
            var specialFoldersLength = specialFolders.Count;

            for (var i = 0; i < specialFoldersLength; i++)
            {
                if (specialFolders[i].Length == 0) continue;

                var pathSpecialFolder = GetCompleteAssetPath(specialFolders[i]);

                if (AssetDatabase.IsValidFolder(pathSpecialFolder)) continue;

                var splitFolderPath = GetSplitAssetPath(pathSpecialFolder);
                var folderName = splitFolderPath[splitFolderPath.Length - 1];
                var folderPath = GetPath(splitFolderPath);

                SetErrorData(_icon.errorSpecialFolder,
                    ErrorType.SpecialFolderNotExists,
                    AssetType.Folder,
                    folderName,
                    folderPath,
                    string.Empty);
            }
        }

        private static void ControlFolders()
        {
            InitializeValidator();

            if (_rules == null) return;

            var appPath = Application.dataPath;
            var indexPatternOfNaming = _rules._patternFolders;
            var patternOfNaming = GetPatternOfNaming(indexPatternOfNaming, AssetType.Folder);
            var regexPatternOfNaming = new Regex(patternOfNaming);
            var folders = Directory.GetDirectories(appPath, "*", SearchOption.AllDirectories);

#region [CONTROL NAMING]

            for (var i = 0; i < folders.Length; i++)
            {
                var splitFolderPath = GetSplitAssetPath(folders[i]);
                var folderName = splitFolderPath[splitFolderPath.Length - 1];

                if (splitFolderPath[0].Equals(UNITY_PACKAGES)) continue;

                if (IsInIgnoreFolder(splitFolderPath)) continue;

                if (regexPatternOfNaming.IsMatch(folderName)) continue;

                var folderPath = GetPath(splitFolderPath);

                SetErrorData(_icon.errorWrongName,
                    ErrorType.FolderNameError,
                    AssetType.Folder,
                    folderName,
                    folderPath,
                    NamingPatterns[indexPatternOfNaming]);
            }

#endregion
        }

        private static void ControlPrefabs()
        {
            InitializeValidator();

            if (_rules == null) return;

            const string FILTER_PREFAB = "t:Prefab";

            var conditionPaths = ControlConditions(AssetType.Prefab,
                FILTER_PREFAB,
                Array.IndexOf(ConditionsTypes, "Prefab"));

            ControlLocationAndNaming(AssetType.Prefab,
                FILTER_PREFAB,
                _rules,
                conditionPaths,
                ErrorType.PrefabLocationError,
                ErrorType.PrefabNameError);

            conditionPaths.Clear();
        }

        private static void ControlScripts()
        {
            InitializeValidator();

            if (_rules == null) return;

            const string FILTER_SCRIPT = "t:Script";

            var conditionPaths = ControlConditions(AssetType.Script,
                FILTER_SCRIPT,
                Array.IndexOf(ConditionsTypes, "Script"));

            ControlLocationAndNaming(AssetType.Script,
                FILTER_SCRIPT,
                _rules,
                conditionPaths,
                ErrorType.ScriptLocationError,
                ErrorType.ScriptNameError);

            conditionPaths.Clear();
        }

        private static void ControlTextures()
        {
            InitializeValidator();

            if (_rules == null) return;

            const string FILTER_TEXTURE = "t:Texture";

            var conditionPaths = ControlConditions(AssetType.Texture,
                FILTER_TEXTURE,
                Array.IndexOf(ConditionsTypes, "Texture"));

            ControlLocationAndNaming(AssetType.Texture,
                FILTER_TEXTURE,
                _rules,
                conditionPaths,
                ErrorType.TextureLocationError,
                ErrorType.TextureNameError);

            conditionPaths.Clear();
        }

        private static void ControlScenes()
        {
            InitializeValidator();

            if (_rules == null) return;

            const string FILTER_SCENE = "t:Scene";

            var conditionPaths = ControlConditions(AssetType.Scene,
                FILTER_SCENE,
                Array.IndexOf(ConditionsTypes, "Scene"));

            ControlLocationAndNaming(AssetType.Scene,
                FILTER_SCENE,
                _rules,
                conditionPaths,
                ErrorType.SceneLocationError,
                ErrorType.SceneNameError);

            conditionPaths.Clear();
        }

        private static void ControlSounds()
        {
            InitializeValidator();

            if (_rules == null) return;

            const string FILTER_AUDIO_CLIP = "t:AudioClip";

            var conditionPaths = ControlConditions(AssetType.Sound,
                FILTER_AUDIO_CLIP,
                Array.IndexOf(ConditionsTypes, "Sound"));

            ControlLocationAndNaming(AssetType.Sound,
                FILTER_AUDIO_CLIP,
                _rules,
                conditionPaths,
                ErrorType.SoundLocationError,
                ErrorType.SoundNameError);

            conditionPaths.Clear();
        }

        private static void ControlModels()
        {
            InitializeValidator();

            if (_rules == null) return;

            const string FILTER_MODEL = "t:Model";

            var conditionPaths = ControlConditions(AssetType.Model,
                FILTER_MODEL,
                Array.IndexOf(ConditionsTypes, "Graphics3D"));

            ControlLocationAndNaming(AssetType.Model,
                FILTER_MODEL,
                _rules,
                conditionPaths,
                ErrorType.ModelLocationError,
                ErrorType.ModelNameError);

            conditionPaths.Clear();
        }

        private static void ControlMaterials()
        {
            InitializeValidator();

            if (_rules == null) return;

            const string FILTER_MATERIAL = "t:Material,t:Shader";

            var conditionPaths = ControlConditions(AssetType.Material,
                FILTER_MATERIAL,
                Array.IndexOf(ConditionsTypes, "Material"));

            ControlLocationAndNaming(AssetType.Material,
                FILTER_MATERIAL,
                _rules,
                conditionPaths,
                ErrorType.MaterialLocationError,
                ErrorType.MaterialNameError);

            conditionPaths.Clear();
        }

        private static void ControlAnimations()
        {
            InitializeValidator();

            if (_rules == null) return;

            const string FILTER_ANIMATION = "t:AnimationClip,t:AnimatorController";

            var conditionPaths = ControlConditions(AssetType.Animation,
                FILTER_ANIMATION,
                Array.IndexOf(ConditionsTypes, "Animation"));

            ControlLocationAndNaming(AssetType.Animation,
                FILTER_ANIMATION,
                _rules,
                conditionPaths,
                ErrorType.AnimationLocationError,
                ErrorType.AnimationNameError);

            conditionPaths.Clear();
        }

        private static List<string> ControlConditions(AssetType assetType,
            string filter,
            int conditionAssetIndex)
        {
            var conditionPaths = new List<string>();

            if (!_filter.controlConditions) return conditionPaths;

            var isAssetTypeInCondition = new bool[12];

            for (var i = 0; i < _rules._conditionFormula.Count; i++)
            {
                // Int to Bool array
                for (var j = 0; j < isAssetTypeInCondition.Length; j++)
                {
                    isAssetTypeInCondition[j] = ((_rules._conditionSelection[i] >> j) & 1) == 1;
                }

                if (!isAssetTypeInCondition[conditionAssetIndex]) continue;

                var splitActualCondition = GetSplitCondition(_rules._conditionFormula[i]);

                if (splitActualCondition.Length != 1
                    && splitActualCondition.Length != 2) continue;

                if (splitActualCondition.Length == 2)
                {
                    var countSpecialChar = _rules._conditionFormula[i].Split('*').Length - 1;
                    if (countSpecialChar > 1) continue;
                }

                var pathSectionOne = GetCompleteAssetPath(splitActualCondition[0]);

                if (AssetDatabase.IsValidFolder(pathSectionOne))
                {
                    var subFolders = AssetDatabase.GetSubFolders(pathSectionOne);

                    if (subFolders.Length == 0)
                    {
                        var guids = AssetDatabase.FindAssets(filter,
                            new[] {pathSectionOne});

                        if (guids.Length == 0)
                        {
                            SetErrorData(_icon.errorNotContain,
                                ErrorType.FolderNotContain,
                                assetType,
                                GetMessageMissingAsset(assetType),
                                pathSectionOne.ModifyPathSeparators(),
                                string.Empty);
                        }
                        else
                        {
                            var conditionPath = pathSectionOne.ModifyPathSeparators();
                            conditionPaths.Add(conditionPath);
                        }
                    }
                    else
                    {
                        for (var j = 0; j < subFolders.Length; j++)
                        {
                            var parentFolderPath = GetParentFolderPath(splitActualCondition,
                                subFolders[j]);

                            if (AssetDatabase.IsValidFolder(parentFolderPath))
                            {
                                var guids = AssetDatabase.FindAssets(filter,
                                    new[] {parentFolderPath});

                                if (guids.Length == 0)
                                {
                                    SetErrorData(_icon.errorNotContain,
                                        ErrorType.FolderNotContain,
                                        assetType,
                                        GetMessageMissingAsset(assetType),
                                        parentFolderPath.ModifyPathSeparators(),
                                        string.Empty);
                                }
                                else
                                {
                                    var conditionPath = parentFolderPath.ModifyPathSeparators();
                                    conditionPaths.Add(conditionPath);
                                }
                            }
                            else
                            {
                                SetErrorData(_icon.errorNotValid,
                                    ErrorType.FolderNotValid,
                                    AssetType.Folder,
                                    GetMessageConditionRow(i, _rules._conditionFormula[i]),
                                    parentFolderPath.ModifyPathSeparators(),
                                    string.Empty);
                            }
                        }
                    }
                }
                else
                {
                    SetErrorData(_icon.errorNotValid,
                        ErrorType.FolderNotValid,
                        AssetType.Folder,
                        GetMessageConditionRow(i, _rules._conditionFormula[i]),
                        pathSectionOne,
                        string.Empty);
                }
            }

            return conditionPaths;
        }

        private static void ControlLocationAndNaming(AssetType assetType,
            string filter,
            DValidatorRules rules,
            IList<string> conditionPaths,
            ErrorType locationError,
            ErrorType nameError)
        {
            var indexPatternOfNaming = 0;
            string[] splitAssetRootFolders = { };

            switch (assetType)
            {
                case AssetType.Folder:
                    break;
                case AssetType.Scene:
                    indexPatternOfNaming = rules._patternScenes;
                    splitAssetRootFolders = rules._rootFolderScenes.SplitRootFolders();
                    break;
                case AssetType.Prefab:
                    indexPatternOfNaming = rules._patternPrefabs;
                    splitAssetRootFolders = rules._rootFolderPrefabs.SplitRootFolders();
                    break;
                case AssetType.Script:
                    indexPatternOfNaming = rules._patternScripts;
                    splitAssetRootFolders = rules._rootFolderScripts.SplitRootFolders();
                    break;
                case AssetType.Texture:
                    indexPatternOfNaming = rules._patternTextures;
                    splitAssetRootFolders = rules._rootFolderTextures.SplitRootFolders();
                    break;
                case AssetType.Model:
                    indexPatternOfNaming = rules._patternGraphics3D;
                    splitAssetRootFolders = rules._rootFolderGraphics3D.SplitRootFolders();
                    break;
                case AssetType.Sound:
                    indexPatternOfNaming = rules._patternSounds;
                    splitAssetRootFolders = rules._rootFolderSounds.SplitRootFolders();
                    break;
                case AssetType.Material:
                    indexPatternOfNaming = rules._patternMaterials;
                    splitAssetRootFolders = rules._rootFolderMaterials.SplitRootFolders();
                    break;
                case AssetType.Animation:
                    indexPatternOfNaming = rules._patternAnimations;
                    splitAssetRootFolders = rules._rootFolderAnimations.SplitRootFolders();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("assetType", assetType, null);
            }

            var patternOfNaming = GetPatternOfNaming(indexPatternOfNaming, assetType);
            var regexPatternOfNaming = new Regex(patternOfNaming);
            var assetGuids = AssetDatabase.FindAssets(filter, null);

            for (var i = 0; i < assetGuids.Length; i++)
            {
                var actualAssetPath = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                var splitActualAssetPath = GetSplitAssetPath(actualAssetPath);

                if (splitActualAssetPath[0].Equals(UNITY_PACKAGES)) continue;

                if (IsInIgnoreFolder(splitActualAssetPath)) continue;

                var assetName = splitActualAssetPath[splitActualAssetPath.Length - 1];
                var isAssetInMainFolder = false;
                var folderPath = GetPath(splitActualAssetPath);

                //--- Control Location -------------------------------------------------------------
                for (var j = 0; j < splitActualAssetPath.Length - 1; j++)
                {
                    for (var k = 0; k < splitAssetRootFolders.Length; k++)
                    {
                        if (!splitActualAssetPath[j].Equals(splitAssetRootFolders[k])) continue;
                        isAssetInMainFolder = true;
                        break;
                    }

                    for (var l = 0; l < conditionPaths.Count; l++)
                    {
                        if (!folderPath.Equals(conditionPaths[l])) continue;
                        isAssetInMainFolder = true;
                        break;
                    }
                }

                if (!isAssetInMainFolder)
                {
                    SetErrorData(_icon.errorWrongLocation,
                        locationError,
                        assetType,
                        assetName,
                        folderPath,
                        GetLocationErrorMessage(_rules._rootFolderTextures));
                }

                //--- Control Name -----------------------------------------------------------------
                if (regexPatternOfNaming.IsMatch(assetName)) continue;

                SetErrorData(_icon.errorWrongName,
                    nameError,
                    assetType,
                    assetName,
                    folderPath,
                    NamingPatterns[indexPatternOfNaming]);
            }

            conditionPaths.Clear();
        }

#endregion

#region [DRAW METHODS]

        [MenuItem("Tools/Validator")]
        private static void UnityMenuTossValidator()
        {
            InitializeValidator();
            DrawMainWindow();
        }

        private static void DrawMainWindow()
        {
            _mainWindow = (TossValidator) GetWindow(typeof(TossValidator));
            _mainWindow.minSize = new Vector2(1024, 400);
            _mainWindow.titleContent.image = _icon.editorWindow;
            _mainWindow.titleContent.text = "Validator";
            _mainWindow.titleContent.tooltip = "Tool for controlling assets";
            _mainWindow.Show();
        }

        private static void DrawValidator(float windowWidth)
        {
            GUILayout.Space(50f);

#region [FILTER PANEL]

            _guiValue.styleDynamicMarginLeft.margin.top = 0;
            _guiValue.styleDynamicMarginLeft.margin.left = (int) ((windowWidth - 960) / 2);

            EditorGUILayout.BeginHorizontal(_guiValue.styleDynamicMarginLeft,
                GUILayout.Width(840), GUILayout.Height(40));

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox,
                GUILayout.Width(800), GUILayout.Height(40));

            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling);

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginVertical();
            _filter.controlSpecialFolders = EditorGUILayout.ToggleLeft("Special Folders",
                _filter.controlSpecialFolders,
                GUILayout.Width(120),
                GUILayout.Height(20));

            _filter.controlScenes = EditorGUILayout.ToggleLeft("Scenes",
                _filter.controlScenes,
                GUILayout.Width(120),
                GUILayout.Height(20));
            EditorGUILayout.EndVertical();

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginVertical();
            _filter.controlFolders = EditorGUILayout.ToggleLeft("All Folders",
                _filter.controlFolders,
                GUILayout.Width(120),
                GUILayout.Height(20));

            _filter.controlGraphics3D = EditorGUILayout.ToggleLeft("3D (Models)",
                _filter.controlGraphics3D,
                GUILayout.Width(120),
                GUILayout.Height(20));
            EditorGUILayout.EndVertical();

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginVertical();
            _filter.controlPrefabs = EditorGUILayout.ToggleLeft("Prefabs",
                _filter.controlPrefabs,
                GUILayout.Width(100),
                GUILayout.Height(20));

            _filter.controlSounds = EditorGUILayout.ToggleLeft("Sounds",
                _filter.controlSounds,
                GUILayout.Width(100),
                GUILayout.Height(20));
            EditorGUILayout.EndVertical();

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginVertical();
            _filter.controlScripts = EditorGUILayout.ToggleLeft("Scripts",
                _filter.controlScripts,
                GUILayout.Width(100),
                GUILayout.Height(20));

            _filter.controlTextures = EditorGUILayout.ToggleLeft("Textures",
                _filter.controlTextures,
                GUILayout.Width(100),
                GUILayout.Height(20));

            EditorGUILayout.EndVertical();

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginVertical();
            _filter.controlMaterials = EditorGUILayout.ToggleLeft("Materials/Shaders",
                _filter.controlMaterials,
                GUILayout.Width(160),
                GUILayout.Height(20));

            _filter.controlAnimations = EditorGUILayout.ToggleLeft("Animations/Controllers",
                _filter.controlAnimations,
                GUILayout.Width(160),
                GUILayout.Height(20));
            EditorGUILayout.EndVertical();

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginVertical();
            _filter.controlConditions = EditorGUILayout.ToggleLeft("Conditions",
                _filter.controlConditions,
                GUILayout.Width(120),
                GUILayout.Height(18));
            EditorGUILayout.EndVertical();

            // Button All - select all          
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button(new GUIContent("All", "Select All"),
                GUILayout.Width(50),
                GUILayout.Height(18)))
            {
                _filter.controlSpecialFolders = true;
                _filter.controlFolders = true;
                _filter.controlPrefabs = true;
                _filter.controlScripts = true;
                _filter.controlTextures = true;
                _filter.controlScenes = true;
                _filter.controlGraphics3D = true;
                _filter.controlSounds = true;
                _filter.controlMaterials = true;
                _filter.controlAnimations = true;

                _filter.controlConditions = true;
            }

            // Button None - deselect all
            if (GUILayout.Button(new GUIContent("None", "Deselect All"),
                GUILayout.Width(50),
                GUILayout.Height(19)))
            {
                _filter.controlSpecialFolders = false;
                _filter.controlFolders = false;
                _filter.controlPrefabs = false;
                _filter.controlScripts = false;
                _filter.controlTextures = false;
                _filter.controlScenes = false;
                _filter.controlGraphics3D = false;
                _filter.controlSounds = false;
                _filter.controlMaterials = false;
                _filter.controlAnimations = false;

                _filter.controlConditions = false;
            }

            EditorGUILayout.EndVertical();

            GUI.color = new Color(0.5f, 1.0f, 0.5f);
            if (GUILayout.Button(new GUIContent("CONTROL PROJECT", "Control Project"),
                GUILayout.Width(150),
                GUILayout.Height(40)))
            {
                SaveValidatorStates();
                ControlProject();
            }

            GUI.color = Color.white;

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

#endregion

#region [MIDDLE PANEL]

            _guiValue.styleDynamicMarginLeft.margin.top = 0;
            _guiValue.styleDynamicMarginLeft.margin.left = (int) ((windowWidth - 960) / 2);

            EditorGUILayout.BeginHorizontal(_guiValue.styleDynamicMarginLeft,
                GUILayout.Width(960));

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginHorizontal(GUILayout.Width(304));
            EditorGUILayout.HelpBox("TIME OF CONTROL: " + _controlTime + " ms",
                MessageType.Info,
                true);
            EditorGUILayout.EndHorizontal();

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginHorizontal(GUILayout.Width(340));
            if (_countErrors == 0)
            {
                EditorGUILayout.HelpBox("ERRORS IN ASSETS FOLDER: 0",
                    MessageType.Info,
                    true);
            }
            else
            {
                string errorMessage;
                if (_countErrors <= _settings.countDisplayErrors)
                {
                    errorMessage = "ERRORS IN ASSETS FOLDER: " + _countErrors;
                    EditorGUILayout.HelpBox(errorMessage, MessageType.Error, true);
                }
                else
                {
                    errorMessage = "ERRORS IN ASSETS FOLDER: ";
                    errorMessage += _settings.countDisplayErrors;
                    errorMessage += "+";
                    EditorGUILayout.HelpBox(errorMessage, MessageType.Error, true);
                }
            }

            EditorGUILayout.EndHorizontal();

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Width(304));
            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling || Errors.Count == 0);

            EditorGUILayout.BeginHorizontal(_guiValue.styleExportButtonMarginLeft,
                GUILayout.Width(200),
                GUILayout.Height(26));

            if (GUILayout.Button(
                new GUIContent("Export results to CSV file", "Export results (errors) to CSV file"),
                GUILayout.Width(200),
                GUILayout.Height(26)))
            {
                ExportErrorsToFile();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20f);

#endregion

#region [RESULTS / ROWS]

            // Horizontal line / Separator
            EditorGUI.DrawRect(new Rect(0, 160, windowWidth, 1), Color.black);

            _guiValue.scrollPosValidator =
                EditorGUILayout.BeginScrollView(_guiValue.scrollPosValidator);

            if (Errors.Count == 0)
            {
                _guiValue.styleDynamicMarginLeft.margin.top = 50;
                _guiValue.styleDynamicMarginLeft.margin.left = (int) ((windowWidth - 340) / 2);

                EditorGUILayout.BeginHorizontal(_guiValue.styleDynamicMarginLeft,
                    GUILayout.Width(340));

                EditorGUILayout.HelpBox(GetWarningMessageAfterCompile(),
                    MessageType.Info,
                    true);

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                var loopLength = _countErrors <= _settings.countDisplayErrors
                    ? _countErrors
                    : _settings.countDisplayErrors;

                for (var i = 0; i < loopLength; i++)
                {
                    DrawRow(i);
                }
            }

            EditorGUILayout.EndScrollView();

#endregion

            DrawBottomPanel();
        }

        private static void DrawRules()
        {
            GUILayout.Space(50f);

            _guiValue.scrollPosRules =
                EditorGUILayout.BeginScrollView(_guiValue.scrollPosRules);

#region [PATTERNS]

            _guiValue.foldoutPatterns = EditorGUILayout.Foldout(
                _guiValue.foldoutPatterns,
                _guiValue.titlePatterns,
                true);

            if (_guiValue.foldoutPatterns)
            {
                EditorGUILayout.BeginHorizontal();

                //----------------------------------------------------------------------------------
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                _rules._patternFolders = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Folder),
                    _rules._patternFolders,
                    NamingPatterns);

                _rules._patternPrefabs = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Prefab),
                    _rules._patternPrefabs,
                    NamingPatterns);

                EditorGUILayout.LabelField(GetFormattedLabelAssetType(AssetType.Script),
                    NamingPatterns[0]);

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();

                //----------------------------------------------------------------------------------
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                _rules._patternTextures = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Texture),
                    _rules._patternTextures,
                    NamingPatterns);

                _rules._patternScenes = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Scene),
                    _rules._patternScenes,
                    NamingPatterns);

                _rules._patternGraphics3D = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Model),
                    _rules._patternGraphics3D,
                    NamingPatterns);

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();

                //----------------------------------------------------------------------------------
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                _rules._patternSounds = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Sound),
                    _rules._patternSounds,
                    NamingPatterns);

                _rules._patternMaterials = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Material),
                    _rules._patternMaterials,
                    NamingPatterns);

                _rules._patternAnimations = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Animation),
                    _rules._patternAnimations,
                    NamingPatterns);

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }

#endregion

            GUILayout.Space(20f);

#region [MAIN ROOT FOLDERS]

            _guiValue.foldoutRootFolders = EditorGUILayout.Foldout(
                _guiValue.foldoutRootFolders,
                _guiValue.titleRootFolders,
                true,
                EditorStyles.foldout);

            if (_guiValue.foldoutRootFolders)
            {
                EditorGUILayout.BeginHorizontal();

                //----------------------------------------------------------------------------------
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                _rules._rootFolderPrefabs =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Prefab),
                        _rules._rootFolderPrefabs);

                _rules._rootFolderScripts =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Script),
                        _rules._rootFolderScripts);

                _rules._rootFolderTextures =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Texture),
                        _rules._rootFolderTextures);

                _rules._rootFolderScenes =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Scene),
                        _rules._rootFolderScenes);

                EditorGUILayout.LabelField(" ", GetInfoTextRootFolders());

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();

                //----------------------------------------------------------------------------------
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                _rules._rootFolderGraphics3D =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Model),
                        _rules._rootFolderGraphics3D);

                _rules._rootFolderSounds =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Sound),
                        _rules._rootFolderSounds);

                _rules._rootFolderMaterials =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Material),
                        _rules._rootFolderMaterials);

                _rules._rootFolderAnimations =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Animation),
                        _rules._rootFolderAnimations);

                EditorGUILayout.LabelField(" ", GetInfoTextRootFolders());

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }

#endregion

            GUILayout.Space(20f);

#region [SPECIAL FOLDERS]

            _guiValue.foldoutSpecialFolders = EditorGUILayout.Foldout(
                _guiValue.foldoutSpecialFolders,
                _guiValue.titleSpecialFolders,
                true);

            if (_guiValue.foldoutSpecialFolders)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                // Add button
                GUI.color = _guiValue.addButtonColor;
                if (GUILayout.Button("Add Special Folder", GUILayout.Height(24)))
                {
                    if (_rules._specialFolders.Count < _settings.countSpecialFolders)
                    {
                        _rules._specialFolders.Add(string.Empty);
                    }
                }

                GUI.color = Color.white;

                // List of Special Folders
                for (var i = 0; i < _rules._specialFolders.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        _rules._specialFolders[i] =
                            EditorGUILayout.TextField(_guiValue.labelSpecialFolder,
                                _rules._specialFolders[i]);

                        GUI.color = _guiValue.deleteButtonColor;
                        if (GUILayout.Button(new GUIContent("X", "Close"), GUILayout.Width(22)))
                        {
                            _rules._specialFolders.RemoveAt(i);
                        }

                        GUI.color = Color.white;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.LabelField(" ", GetInfoTextSpecialFolders());

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
            }

#endregion

            GUILayout.Space(20f);

#region [IGNORE FOLDERS]

            _guiValue.foldoutIgnoreFolders = EditorGUILayout.Foldout(
                _guiValue.foldoutIgnoreFolders,
                _guiValue.titleIgnoreFolders,
                true);

            if (_guiValue.foldoutIgnoreFolders)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                // Add button
                GUI.color = _guiValue.addButtonColor;
                if (GUILayout.Button("Add Ignore Folder", GUILayout.Height(24)))
                {
                    if (_rules._ignoreFolders.Count < _settings.countIgnoreFolders)
                        _rules._ignoreFolders.Add(string.Empty);
                }

                GUI.color = Color.white;

                // List of ignore folders
                for (var i = 0; i < _rules._ignoreFolders.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        _rules._ignoreFolders[i] =
                            EditorGUILayout.TextField(_guiValue.labelIgnoreFolder,
                                _rules._ignoreFolders[i]);

                        GUI.color = _guiValue.deleteButtonColor;
                        if (GUILayout.Button(new GUIContent("X", "Close"), GUILayout.Width(22)))
                        {
                            _rules._ignoreFolders.RemoveAt(i);
                        }

                        GUI.color = Color.white;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.LabelField(" ", GetInfoTextIgnoreFolders());

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
            }

#endregion

            EditorGUILayout.EndScrollView();

            DrawBottomPanel();

            EditorUtility.SetDirty(_rules);
        }

        private static void DrawConditions()
        {
            GUILayout.Space(50f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space();

            // Add button
            GUI.color = _guiValue.addButtonColor;
            if (GUILayout.Button("Add Condition", GUILayout.Height(24)))
            {
                if (_rules._conditionFormula.Count < _settings.countConditionRows)
                {
                    _rules._conditionFormula.Add(string.Empty);
                    _rules._conditionSelection.Add(0);
                }
            }

            GUI.color = Color.white;

            DrawConditionRow();

            EditorGUILayout.LabelField(" ", GetInfoTextConditions());

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox("Example 1: GameAssets\\Units\\*", MessageType.Info);
            EditorGUILayout.HelpBox("Example 2: GameAssets\\Units\\*\\Prefabs", MessageType.Info);
            EditorGUILayout.HelpBox("Example 3: GameAssets\\Enemies\\Prefabs", MessageType.Info);
            EditorGUILayout.EndHorizontal();

            DrawBottomPanel();
        }

        private static void DrawConditionRow()
        {
            for (var i = 0; i < _rules._conditionFormula.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var countSpecialChar = _rules._conditionFormula[i].Split('*').Length - 1;

                if (countSpecialChar == 0 || countSpecialChar == 1)
                {
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = Color.red;
                }

                _rules._conditionFormula[i] = EditorGUILayout.TextField(
                    GetFormattedLabelCondition(i),
                    _rules._conditionFormula[i]);
                GUI.color = Color.white;

                _rules._conditionSelection[i] = EditorGUILayout.MaskField(
                    _rules._conditionSelection[i],
                    ConditionsTypes,
                    GUILayout.Width(150));

                //--- Button move row up -----------------------------------------------------------
                if (GUILayout.Button(_icon.arrowUp, GUILayout.Width(22)))
                {
                    if (i != 0)
                    {
                        var exchangeHelper1 = _rules._conditionFormula[i];
                        var exchangeHelper2 = _rules._conditionSelection[i];

                        _rules._conditionFormula[i] = _rules._conditionFormula[i - 1];
                        _rules._conditionSelection[i] = _rules._conditionSelection[i - 1];

                        _rules._conditionFormula[i - 1] = exchangeHelper1;
                        _rules._conditionSelection[i - 1] = exchangeHelper2;
                    }
                }

                //--- Button move row down ---------------------------------------------------------
                if (GUILayout.Button(_icon.arrowDown, GUILayout.Width(22)))
                {
                    if (i != _rules._conditionFormula.Count - 1)
                    {
                        var exchangeHelper1 = _rules._conditionFormula[i];
                        var exchangeHelper2 = _rules._conditionSelection[i];

                        _rules._conditionFormula[i] = _rules._conditionFormula[i + 1];
                        _rules._conditionSelection[i] = _rules._conditionSelection[i + 1];

                        _rules._conditionFormula[i + 1] = exchangeHelper1;
                        _rules._conditionSelection[i + 1] = exchangeHelper2;
                    }
                }

                GUI.color = _guiValue.duplicateButtonColor;

                //--- Button duplicate row ---------------------------------------------------------
                if (GUILayout.Button(new GUIContent("D", "Duplicate row"), GUILayout.Width(22)))
                {
                    if (_rules._conditionFormula.Count < _settings.countConditionRows)
                    {
                        _rules._conditionFormula.Insert(i, _rules._conditionFormula[i]);
                        _rules._conditionSelection.Insert(i, _rules._conditionSelection[i]);
                    }
                }

                GUI.color = _guiValue.deleteButtonColor;

                //--- Button delete row ------------------------------------------------------------
                if (GUILayout.Button(new GUIContent("X", "Delete row"), GUILayout.Width(22)))
                {
                    _rules._conditionFormula.RemoveAt(i);
                    _rules._conditionSelection.RemoveAt(i);
                }

                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();
            }
        }

        private static void DrawSettings(float windowWidth)
        {
            GUILayout.Space(50f);

            _guiValue.styleDynamicMarginLeft.margin.top = 0;
            _guiValue.styleDynamicMarginLeft.margin.left = (int) ((windowWidth - 600) / 2);

            EditorGUILayout.BeginHorizontal(_guiValue.styleDynamicMarginLeft);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(600));
            EditorGUILayout.Space();

            _settings.countDisplayErrors = EditorGUILayout.IntSlider("Count Display Errors:",
                _settings.countDisplayErrors,
                10,
                MAX_COUNT_DISPLAY_ERRORS);

            EditorGUILayout.Space();

            _settings.countSpecialFolders = EditorGUILayout.IntSlider("Count Special Folders:",
                _settings.countSpecialFolders,
                10,
                MAX_COUNT_SPECIAL_FOLDERS);

            EditorGUILayout.Space();

            _settings.countIgnoreFolders = EditorGUILayout.IntSlider("Count Ignore Folders:",
                _settings.countIgnoreFolders,
                10,
                MAX_COUNT_IGNORE_FOLDERS);

            EditorGUILayout.Space();

            _settings.countConditionRows = EditorGUILayout.IntSlider("Count Condition Rows:",
                _settings.countConditionRows,
                10,
                MAX_COUNT_CONDITION_ROWS);

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            SaveSettings();

            ControlActualStatesCompareToSettings();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            DrawBottomPanel();
        }

        private static void DrawRow(int index)
        {
            var assetName = Errors[index].assetName;
            var assetPath = Errors[index].assetPath;
            var errorType = Errors[index].errorType;
            var errorName = Errors[index].errorName;
            var assetType = Errors[index].assetType;
            var correctPattern = Errors[index].correctPattern;

            var rowStyle = index % 2 == 0 ? _guiValue.styleEvenRow : _guiValue.styleOddRow;
            EditorGUILayout.BeginHorizontal(rowStyle, GUILayout.Height(50));

            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling);

            if (errorType == ErrorType.SpecialFolderNotExists
                || errorType == ErrorType.FolderNotValid)
            {
                GUILayout.Box(Errors[index].errorIcon,
                    rowStyle,
                    GUILayout.Width(50),
                    GUILayout.Height(50));
            }
            else
            {
                // Left Row Button
                if (GUILayout.Button(Errors[index].errorIcon,
                    GUILayout.Width(50),
                    GUILayout.Height(50)))
                {
                    switch (errorType)
                    {
                        case ErrorType.FolderNameError:
                        case ErrorType.PrefabNameError:
                        case ErrorType.PrefabLocationError:
                        case ErrorType.ScriptNameError:
                        case ErrorType.ScriptLocationError:
                        case ErrorType.TextureNameError:
                        case ErrorType.TextureLocationError:
                        case ErrorType.SceneNameError:
                        case ErrorType.SceneLocationError:
                        case ErrorType.SoundNameError:
                        case ErrorType.SoundLocationError:
                        case ErrorType.ModelNameError:
                        case ErrorType.ModelLocationError:
                        case ErrorType.MaterialNameError:
                        case ErrorType.MaterialLocationError:
                        case ErrorType.AnimationNameError:
                        case ErrorType.AnimationLocationError:
                        {
                            var path = GetFormattedAssetPath(assetPath, assetName);
                            SelectAsset(path);
                            break;
                        }
                        case ErrorType.FolderNotContain:
                        {
                            SelectAsset(assetPath);
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginVertical();
            {
                // Top label
                EditorGUILayout.LabelField(errorName,
                    assetName,
                    EditorStyles.boldLabel,
                    GUILayout.MinWidth(640),
                    GUILayout.Height(20));

                var outputAssetPath = assetPath.AddSuffixMissingFolders(assetType, errorType);

                // Bottom label
                EditorGUILayout.LabelField(assetType.ToString(), outputAssetPath,
                    EditorStyles.label,
                    GUILayout.MinWidth(640),
                    GUILayout.Height(20));
            }
            EditorGUILayout.EndVertical();

            switch (errorType)
            {
                case ErrorType.SpecialFolderNotExists:

                    EditorGUILayout.HelpBox("Create Folder(s) in the Project Window",
                        MessageType.Info,
                        false);

                    break;
                case ErrorType.FolderNameError:
                case ErrorType.PrefabNameError:
                case ErrorType.ScriptNameError:
                case ErrorType.TextureNameError:
                case ErrorType.SceneNameError:
                case ErrorType.SoundNameError:
                case ErrorType.ModelNameError:
                case ErrorType.MaterialNameError:
                case ErrorType.AnimationNameError:

                    EditorGUILayout.HelpBox("Correct: " + correctPattern,
                        MessageType.Info,
                        false);

                    break;
                case ErrorType.PrefabLocationError:
                case ErrorType.ScriptLocationError:
                case ErrorType.TextureLocationError:
                case ErrorType.SceneLocationError:
                case ErrorType.SoundLocationError:
                case ErrorType.ModelLocationError:
                case ErrorType.MaterialLocationError:
                case ErrorType.AnimationLocationError:

                    EditorGUILayout.HelpBox("Check Root folders or Conditions",
                        MessageType.Info,
                        false);

                    break;
                case ErrorType.FolderNotContain:

                    EditorGUILayout.HelpBox(GetHelpBoxText(assetType),
                        MessageType.Info,
                        false);

                    break;
                case ErrorType.FolderNotValid:

                    EditorGUILayout.HelpBox("Create Folder(s) in the Project Window",
                        MessageType.Info,
                        false);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawBottomPanel()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.Label("Created by: SkyToss");
                GUILayout.FlexibleSpace();
                GUILayout.Label("Version: 1.6");
            }
            EditorGUILayout.EndHorizontal();
        }

#endregion

#region [WORK / HELP METHODS]

        private static void InitializeValidator()
        {
            if (_rules != null) return;

            //ConsoleDebugLog("[TOSS VALIDATOR] --> InitializeValidator()");

            InitializeProjectRules();

            LoadValidatorStates();
        }

        private static void InitializeProjectRules()
        {
            var guidValidatorSettings = AssetDatabase.FindAssets("ValidatorSettings");

            if (guidValidatorSettings.Length == 0) return;

            var pathValidatorSettings = AssetDatabase.GUIDToAssetPath(guidValidatorSettings[0]);

            var pathImages = pathValidatorSettings.Replace("ValidatorSettings.asset", "Images/");

            InitializeIconsAndStyles(pathImages);

            _rules = (DValidatorRules) AssetDatabase.LoadAssetAtPath(
                pathValidatorSettings,
                typeof(DValidatorRules));
        }

        private static void InitializeIconsAndStyles(string pathImages)
        {
            _icon.editorWindow = GetTexture2D(pathImages, "icon-editor-window");
            _icon.errorSpecialFolder = GetTexture2D(pathImages, "icon-error-folder-special");
            _icon.errorWrongLocation = GetTexture2D(pathImages, "icon-error-wrong-location");
            _icon.errorWrongName = GetTexture2D(pathImages, "icon-error-wrong-name");
            _icon.errorNotContain = GetTexture2D(pathImages, "icon-error-not-contain");
            _icon.errorNotValid = GetTexture2D(pathImages, "icon-error-not-valid");
            _icon.arrowUp = GetTexture2D(pathImages, "arrow-up");
            _icon.arrowDown = GetTexture2D(pathImages, "arrow-down");

            var styleEvenRow = _guiValue.styleEvenRow;

            styleEvenRow.normal.background = EditorGUIUtility.isProSkin
                ? GetTexture2D(pathImages, "even-row-bg-dark")
                : GetTexture2D(pathImages, "even-row-bg-light");

            styleEvenRow.padding = new RectOffset(10, 6, 6, 6);

            _guiValue.styleOddRow.padding = new RectOffset(10, 6, 6, 6);
            _guiValue.styleExportButtonMarginLeft.margin.left = 48;
        }

        private static void SaveValidatorStates()
        {
            AssetDatabase.SaveAssets();
            SaveFilter();
            SaveSettings();
        }

        private static void SaveFilter()
        {
            EditorPrefs.SetBool("controlSpecialFolders", _filter.controlSpecialFolders);
            EditorPrefs.SetBool("controlFolders", _filter.controlFolders);
            EditorPrefs.SetBool("controlPrefabs", _filter.controlPrefabs);
            EditorPrefs.SetBool("controlScripts", _filter.controlScripts);
            EditorPrefs.SetBool("controlTextures", _filter.controlTextures);
            EditorPrefs.SetBool("controlScenes", _filter.controlScenes);
            EditorPrefs.SetBool("controlGraphics3D", _filter.controlGraphics3D);
            EditorPrefs.SetBool("controlSounds", _filter.controlSounds);
            EditorPrefs.SetBool("controlMaterials", _filter.controlMaterials);
            EditorPrefs.SetBool("controlAnimations", _filter.controlAnimations);

            EditorPrefs.SetBool("controlConditions", _filter.controlConditions);
        }

        private static void SaveSettings()
        {
            EditorPrefs.SetInt("countDisplayErrors", _settings.countDisplayErrors);
            EditorPrefs.SetInt("countSpecialFolders", _settings.countSpecialFolders);
            EditorPrefs.SetInt("countIgnoreFolders", _settings.countIgnoreFolders);
            EditorPrefs.SetInt("countConditionRows", _settings.countConditionRows);
        }

        private static void LoadValidatorStates()
        {
            LoadFilter();
            LoadSettings();
        }

        private static void LoadFilter()
        {
            _filter.controlSpecialFolders = EditorPrefs.GetBool("controlSpecialFolders", true);
            _filter.controlFolders = EditorPrefs.GetBool("controlFolders", true);
            _filter.controlPrefabs = EditorPrefs.GetBool("controlPrefabs", true);
            _filter.controlScripts = EditorPrefs.GetBool("controlScripts", true);
            _filter.controlTextures = EditorPrefs.GetBool("controlTextures", true);
            _filter.controlScenes = EditorPrefs.GetBool("controlScenes", true);
            _filter.controlGraphics3D = EditorPrefs.GetBool("controlGraphics3D", true);
            _filter.controlSounds = EditorPrefs.GetBool("controlSounds", true);
            _filter.controlMaterials = EditorPrefs.GetBool("controlMaterials", true);
            _filter.controlAnimations = EditorPrefs.GetBool("controlAnimations", true);

            _filter.controlConditions = EditorPrefs.GetBool("controlConditions", true);
        }

        private static void LoadSettings()
        {
            _settings.countDisplayErrors = EditorPrefs.GetInt("countDisplayErrors", 40);
            _settings.countSpecialFolders = EditorPrefs.GetInt("countSpecialFolders", 10);
            _settings.countIgnoreFolders = EditorPrefs.GetInt("countIgnoreFolders", 10);
            _settings.countConditionRows = EditorPrefs.GetInt("countConditionRows", 10);
        }

        private static void ExportErrorsToFile()
        {
            if (Errors.Count == 0) return;

            try
            {
                using (var file = new StreamWriter(EXPORT_FILE_PATH, false))
                {
                    const char FILE_SEPARATOR = ',';

                    var sb = new StringBuilder();
                    var countErrors = Errors.Count;

                    // CSV file titles
                    sb.Append("ERROR NAME, ASSET TYPE, ASSET NAME, ASSET PATH");
                    file.WriteLine(sb);
                    sb.Clear();

                    for (var i = 0; i < countErrors; i++)
                    {
                        sb.Append(Errors[i].errorName);
                        sb.Append(FILE_SEPARATOR);
                        sb.Append(Errors[i].assetType);
                        sb.Append(FILE_SEPARATOR);
                        sb.Append(Errors[i].assetName);
                        sb.Append(FILE_SEPARATOR);
                        sb.Append(Errors[i].assetPath);

                        file.WriteLine(sb);

                        sb.Clear();
                    }

                    file.Close();

                    sb.Append("[Validator]");
                    sb.Append(" Export to CSV file was successful! --->");
                    sb.Append(" Path: ");
                    sb.Append(EXPORT_FILE_PATH);

                    ConsoleDebugLog(sb.ToString());

                    sb.Clear();
                }
            }
            catch (Exception ex)
            {
                ConsoleDebugLogError("[Validator] Export Error: " + ex.Message);
            }
        }

        private static void SetErrorData(Texture2D errorIcon,
            ErrorType errorType,
            AssetType assetType,
            string assetName,
            string assetPath,
            string example)
        {
            if (!IsPossibilityDisplayErrors()) return;

            _countErrors++;

            var errorName = GetErrorName(errorType);

            Errors.Add(new Error(errorIcon,
                errorType,
                assetType,
                errorName,
                assetName,
                assetPath,
                example));
        }

        private static bool IsPossibilityDisplayErrors()
        {
            return _countErrors <= _settings.countDisplayErrors;
        }

        private static void ResetErrorData()
        {
            _countErrors = 0;
            Errors.Clear();
        }

        private static void SelectAsset(string assetPath)
        {
            var assetObject = AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEngine.Object));
            Selection.activeObject = assetObject;
        }

        private static string GetLocationErrorMessage(string nameRootFolder)
        {
            var sb = new StringBuilder();
            sb.Append("[Root folder(s): ");
            sb.Append(nameRootFolder);
            sb.Append("] or [Check Conditions]");
            return sb.ToString();
        }

        private static string GetPath(string[] slicePath)
        {
            var sb = new StringBuilder();
            var isAssetsFolder = false;

            for (var i = 0; i < slicePath.Length - 1; i++)
            {
                if (slicePath[i].Equals("Assets") && !isAssetsFolder)
                    isAssetsFolder = true;

                if (!isAssetsFolder) continue;

                if (i == slicePath.Length - 2)
                    sb.Append(slicePath[i]);
                else
                    sb.Append(slicePath[i] + ASSET_PATH_SEPARATOR);
            }

            return sb.ToString();
        }

        private static string GetErrorName(ErrorType errorType)
        {
            switch (errorType)
            {
                case ErrorType.FolderNameError:
                case ErrorType.ScriptNameError:
                case ErrorType.PrefabNameError:
                case ErrorType.SceneNameError:
                case ErrorType.TextureNameError:
                case ErrorType.SoundNameError:
                case ErrorType.ModelNameError:
                case ErrorType.MaterialNameError:
                case ErrorType.AnimationNameError:
                    return "WRONG NAME";
                case ErrorType.PrefabLocationError:
                case ErrorType.ScriptLocationError:
                case ErrorType.SceneLocationError:
                case ErrorType.TextureLocationError:
                case ErrorType.SoundLocationError:
                case ErrorType.ModelLocationError:
                case ErrorType.MaterialLocationError:
                case ErrorType.AnimationLocationError:
                    return "WRONG LOCATION";
                case ErrorType.SpecialFolderNotExists:
                    return "NOT EXISTS";
                case ErrorType.FolderNotContain:
                    return "NOT CONTAIN";
                case ErrorType.FolderNotValid:
                    return "NOT VALID";
                default:
                    return "ERROR";
            }
        }

        private static bool IsInIgnoreFolder(string[] input)
        {
            for (var i = 0; i < _rules._ignoreFolders.Count; i++)
            {
                if (_rules._ignoreFolders[i].Length == 0) continue;

                if (Array.IndexOf(input, _rules._ignoreFolders[i]) > -1)
                    return true;
            }

            return false;
        }

        private static string GetPatternOfNaming(int indexPattern, AssetType assetType)
        {
            var sb = new StringBuilder();

            switch (indexPattern)
            {
                case 0:
                    sb.Append("^[A-Z][a-z0-9]{0,}([A-Z][a-z0-9]{0,})*");
                    break;
                case 1:
                    sb.Append("^[a-z][a-z0-9]{0,}([A-Z][a-z0-9]{0,})*");
                    break;
                case 2:
                    sb.Append("^[a-z][a-z0-9]{0,}([_]{0,1}[a-z0-9]{1,})*");
                    break;
                case 3:
                    sb.Append("^[a-z][a-z0-9]{0,}([-]{0,1}[a-z0-9]{1,})*");
                    break;
                case 4:
                    sb.Append("^[A-Z][a-z0-9]{0,}([_]{0,1}[A-Z0-9][a-z0-9]{1,})*");
                    break;
                default:
                    sb.Append("^[A-Z][a-z0-9]{0,}([A-Z][a-z0-9]{0,})*");
                    break;
            }

            switch (assetType)
            {
                case AssetType.Folder:
                    // No suffix
                    break;
                case AssetType.Prefab:
                    sb.Append(".prefab");
                    break;
                case AssetType.Script:
                    sb.Append("(.cs|.dll)");
                    break;
                case AssetType.Texture:
                case AssetType.Sound:
                case AssetType.Model:
                case AssetType.Material:
                case AssetType.Animation:
                    sb.Append("\\.[a-z0-9]{2,6}");
                    break;
                case AssetType.Scene:
                    sb.Append(".unity");
                    break;
                default:
                    throw new ArgumentOutOfRangeException("assetType", assetType, null);
            }

            sb.Append("$");
            return sb.ToString();
        }

        private static string GetCompleteAssetPath(string actualPath)
        {
            var sb = new StringBuilder();
            sb.Append("Assets");
            sb.Append(ASSET_PATH_SEPARATOR);
            sb.Append(actualPath);
            return sb.ToString();
        }

        private static string GetMessageMissingAsset(AssetType assetType)
        {
            var sb = new StringBuilder();
            sb.Append("[Missing ");
            sb.Append(assetType);
            sb.Append("]");
            return sb.ToString();
        }

        private static string GetMessageConditionRow(int conditionRowIndex, string formula)
        {
            var sb = new StringBuilder();
            sb.Append("[Condition ");
            sb.Append(conditionRowIndex + 1);
            sb.Append(": ");
            sb.Append(formula);
            sb.Append("]");
            return sb.ToString();
        }

        private static string GetFormattedLabelAssetType(AssetType assetType)
        {
            var sb = new StringBuilder();
            sb.Append(' ', 8);

            if (assetType == AssetType.Model)
            {
                sb.Append("3D (Models):");
            }
            else
            {
                sb.Append(assetType);
                sb.Append("s:");
            }

            return sb.ToString();
        }

        private static string GetFormattedLabel(string label)
        {
            var sb = new StringBuilder();
            sb.Append(' ', 8);
            sb.Append(label);
            return sb.ToString();
        }

        private static string GetFormattedLabelCondition(int rowIndex)
        {
            var sb = new StringBuilder();
            sb.Append(' ', 4);
            sb.Append("Condition ");
            sb.Append(rowIndex + 1);
            sb.Append(": Assets\\");
            return sb.ToString();
        }

        private static string GetFormattedAssetPath(string assetPath, string assetName)
        {
            var sb = new StringBuilder();
            sb.Append(assetPath);
            sb.Append(ASSET_PATH_SEPARATOR);
            sb.Append(assetName);
            return sb.ToString();
        }

        private static string GetWarningMessageAfterCompile()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("AFTER COMPILE OUTPUT DATA WILL LOST!");
            sb.AppendLine();
            sb.AppendLine("- Use \"Control Project\" Button Frequently");
            sb.AppendLine();
            sb.AppendLine("- Define Ignore folders in section \"Rules\"");
            sb.AppendLine();
            sb.AppendLine("- Define Conditions in section \"Conditions\"");
            sb.AppendLine();
            return sb.ToString();
        }

        private static string[] GetSplitAssetPath(string assetPath)
        {
            return assetPath.Split(new[] {"\\", "/"}, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string[] GetSplitCondition(string condition)
        {
            return condition.Split(new[] {"\\*\\", "\\*"}, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string GetParentFolderPath(string[] splitCondition, string pathSubFolder)
        {
            var sb = new StringBuilder();
            sb.Append(pathSubFolder);

            if (splitCondition.Length != 2) return sb.ToString();
            sb.Append("/");
            sb.Append(splitCondition[1]);

            return sb.ToString();
        }

        private static string GetHelpBoxText(AssetType assetType)
        {
            switch (assetType)
            {
                case AssetType.Folder:
                    return "Add/Create Folder(s)";
                case AssetType.Scene:
                    return "Add/Create Scene";
                case AssetType.Prefab:
                    return "Add/Create Prefab";
                case AssetType.Script:
                    return "Add/Create Script";
                case AssetType.Texture:
                    return "Add/Create Texture";
                case AssetType.Model:
                    return "Add/Create 3D Model";
                case AssetType.Sound:
                    return "Add/Create Sound";
                case AssetType.Material:
                    return "Add/Create Material or Shader";
                case AssetType.Animation:
                    return "Add/Create Animation or Animator Controller";
                default:
                    throw new ArgumentOutOfRangeException("assetType", assetType, null);
            }
        }

        private static string GetInfoTextRootFolders()
        {
            return "[Separator: | (OR)]";
        }

        private static string GetInfoTextSpecialFolders()
        {
            var sb = new StringBuilder();
            sb.Append("[Separator: \\] [Max. count: ");
            sb.Append(_settings.countSpecialFolders);
            sb.Append("] [Special Folder = folder that the project must contain");
            sb.Append(" (example: Documentation, Controllers, etc.)]");
            return sb.ToString();
        }

        private static string GetInfoTextIgnoreFolders()
        {
            var sb = new StringBuilder();
            sb.Append("[Max. count: ");
            sb.Append(_settings.countIgnoreFolders);
            sb.Append("]");
            return sb.ToString();
        }

        private static string GetInfoTextConditions()
        {
            var sb = new StringBuilder();
            sb.Append("[All subfolders symbol: * (One per condition)] ");
            sb.Append("[Separator: \\] ");
            sb.Append("[Max. count: ");
            sb.Append(_settings.countConditionRows);
            sb.Append("]");
            return sb.ToString();
        }

        private static Texture2D GetTexture2D(string texturePath, string textureName)
        {
            var sb = new StringBuilder();
            sb.Append(texturePath);
            sb.Append(textureName);
            sb.Append(".png");

            return (Texture2D) EditorGUIUtility.Load(sb.ToString());
        }

        private static void ControlActualStatesCompareToSettings()
        {
            if (_rules._specialFolders.Count > _settings.countSpecialFolders)
            {
                _rules._specialFolders.RemoveRange(
                    _settings.countSpecialFolders,
                    _rules._specialFolders.Count - _settings.countSpecialFolders);
            }

            if (_rules._ignoreFolders.Count > _settings.countIgnoreFolders)
            {
                _rules._ignoreFolders.RemoveRange(
                    _settings.countIgnoreFolders,
                    _rules._ignoreFolders.Count - _settings.countIgnoreFolders);
            }

            if (_rules._conditionFormula.Count > _settings.countConditionRows)
            {
                _rules._conditionFormula.RemoveRange(
                    _settings.countConditionRows,
                    _rules._conditionFormula.Count - _settings.countConditionRows);

                _rules._conditionSelection.RemoveRange(
                    _settings.countConditionRows,
                    _rules._conditionSelection.Count - _settings.countConditionRows);
            }
        }

#endregion

#region [STRUCTS]

        private readonly struct Error
        {
            public readonly Texture2D errorIcon;
            public readonly ErrorType errorType;
            public readonly AssetType assetType;
            public readonly string errorName;
            public readonly string assetName;
            public readonly string assetPath;
            public readonly string correctPattern;

            public Error(Texture2D errorIcon,
                ErrorType errorType,
                AssetType assetType,
                string errorName,
                string assetName,
                string assetPath,
                string correctPattern)
            {
                this.errorIcon = errorIcon;
                this.errorType = errorType;
                this.assetType = assetType;
                this.errorName = errorName;
                this.assetName = assetName;
                this.assetPath = assetPath;
                this.correctPattern = correctPattern;
            }
        }

        private struct Filter
        {
            public bool controlSpecialFolders;
            public bool controlFolders;
            public bool controlPrefabs;
            public bool controlScripts;
            public bool controlTextures;
            public bool controlScenes;
            public bool controlGraphics3D;
            public bool controlSounds;
            public bool controlMaterials;
            public bool controlAnimations;
            public bool controlConditions;
        }

        private struct Icon
        {
            public Texture2D editorWindow;
            public Texture2D errorSpecialFolder;
            public Texture2D errorWrongLocation;
            public Texture2D errorWrongName;
            public Texture2D errorNotContain;
            public Texture2D errorNotValid;
            public Texture2D arrowUp;
            public Texture2D arrowDown;
        }

        private struct GuiValue
        {
            public float editorActualWidth;
            public int indexToolbarSelected;

            public bool foldoutPatterns;
            public string titlePatterns;
            public bool foldoutRootFolders;
            public string titleRootFolders;
            public bool foldoutSpecialFolders;
            public string titleSpecialFolders;
            public bool foldoutIgnoreFolders;
            public string titleIgnoreFolders;

            public string labelSpecialFolder;
            public string labelIgnoreFolder;

            public GUIStyle styleEvenRow;
            public GUIStyle styleOddRow;
            public GUIStyle styleDynamicMarginLeft;
            public GUIStyle styleExportButtonMarginLeft;

            public Vector2 scrollPosValidator;
            public Vector2 scrollPosRules;

            public Color addButtonColor;
            public Color deleteButtonColor;
            public Color duplicateButtonColor;
        }

        private struct Settings
        {
            public int countDisplayErrors;
            public int countSpecialFolders;
            public int countIgnoreFolders;
            public int countConditionRows;
        }

#endregion

        private static void ConsoleDebugLog(string message)
        {
            var sb = new StringBuilder();
            sb.Append("<b>");
            sb.Append("<color=#79A6FF>");
            sb.Append(message);
            sb.Append("</color>");
            sb.Append("</b>");

            UnityEngine.Debug.Log(sb);
        }

        private static void ConsoleDebugLogError(string message)
        {
            var sb = new StringBuilder();
            sb.Append("<b>");
            sb.Append("<color=#7B0011>");
            sb.Append(message);
            sb.Append("</color>");
            sb.Append("</b>");

            UnityEngine.Debug.LogError(sb);
        }

        public enum ErrorType
        {
            FolderNameError,
            PrefabNameError,
            PrefabLocationError,
            SpecialFolderNotExists,
            ScriptNameError,
            ScriptLocationError,
            TextureNameError,
            TextureLocationError,
            SceneNameError,
            SceneLocationError,
            FolderNotContain,
            FolderNotValid,
            SoundNameError,
            SoundLocationError,
            ModelNameError,
            ModelLocationError,
            MaterialNameError,
            MaterialLocationError,
            AnimationNameError,
            AnimationLocationError
        }

        public enum AssetType
        {
            Folder,
            Scene,
            Prefab,
            Script,
            Texture,
            Model,
            Sound,
            Material,
            Animation
        }
    }

    /// <summary>
    ///     EXTENSIONS
    /// </summary>
    public static class TossValidatorExtensions
    {
        public static string ModifyPathSeparators(this string input)
        {
            return input.Replace('/', '\\');
        }

        public static string[] SplitRootFolders(this string input)
        {
            return input.Split(new[] {"|"}, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string AddSuffixMissingFolders(this string input,
            TossValidator.AssetType assetType,
            TossValidator.ErrorType errorType)
        {
            if (assetType == TossValidator.AssetType.Folder
                && errorType == TossValidator.ErrorType.FolderNotContain)
            {
                return input + "\\ [Missing Folder(s)]";
            }

            return input;
        }
    }
}