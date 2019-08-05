// =================================================================================================
//     Author:			Tomas "Toss" Szilagyi
//     Date created:	05.04.2018
// =================================================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TossValidator
{
    public class TossValidator : EditorWindow
    {
        private const string ASSET_PATH_SEPARATOR = "\\";
        private const string UNITY_PACKAGES = "Packages";
        private const int MAX_COUNT_DISPLAY_ERRORS = 9999;
        private const int MAX_COUNT_SPECIAL_FOLDERS = 30;
        private const int MAX_COUNT_IGNORE_FOLDERS = 30;
        private const int MAX_COUNT_CONDITION_ROWS = 40;

        #region === PRIVATE VARIABLES ==============================================================

        private static TossValidator m_mainWindow;

        private readonly string[] m_toolbarTitles =
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

        private static DValidatorRules m_rules;

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

        private static Icon m_icon;

        private static Filter m_filter = new Filter
        {
            _controlSpecialFolders = true,
            _controlFolders = true,
            _controlPrefabs = true,
            _controlScripts = true,
            _controlTextures = true,
            _controlScenes = true,
            _controlGraphics3D = true,
            _controlSounds = true,
            _controlMaterials = true,
            _controlAnimations = true
        };

        private static Settings m_settings = new Settings
        {
            _countDisplayErrors = 999,
            _countSpecialFolders = 15,
            _countIgnoreFolders = 15,
            _countConditionRows = 15
        };

        private static GuiValue m_guiValue = new GuiValue
        {
            _foldoutPatterns = true,
            _titlePatterns = "PATTERNS OF NAMING",
            _foldoutRootFolders = true,
            _titleRootFolders = "ROOT FOLDERS",
            _foldoutSpecialFolders = true,
            _titleSpecialFolders = "SPECIAL FOLDERS",
            _foldoutIgnoreFolders = true,
            _titleIgnoreFolders = "IGNORE FOLDERS",

            _labelSpecialFolder = GetFormattedLabel("Path: Assets\\"),
            _labelIgnoreFolder = GetFormattedLabel("Folder name:"),

            _addButtonColor = new Color(0.5f, 0.8f, 1.0f),
            _deleteButtonColor = new Color(1.0f, 0.4f, 0.4f),
            _duplicateButtonColor = new Color(1.0f, 1.0f, 0.2f),

            _styleEvenRow = new GUIStyle(),
            _styleOddRow = new GUIStyle(),
            _styleDynamicMarginLeft = new GUIStyle(),
            _styleExportButtonMarginLeft = new GUIStyle()
        };

        private static int m_countErrors;
        private static long m_controlTime;

        #endregion

        //==========================================================================================
        [InitializeOnLoadMethod]
        public static void OnProjectLoadedInEditor()
        {
            //ConsoleDebugLog("[TOSS VALIDATOR] --> OnProjectLoadedInEditor()");
            InitializeValidator();
        }

        //==========================================================================================
        private void OnGUI()
        {
            m_guiValue._editorActualWidth = EditorGUIUtility.currentViewWidth;

            m_guiValue._indexToolbarSelected = GUI.Toolbar(
                new Rect((m_guiValue._editorActualWidth - 700) / 2, 10, 700, 30),
                m_guiValue._indexToolbarSelected,
                m_toolbarTitles);

            switch (m_guiValue._indexToolbarSelected)
            {
                case 0:
                    DrawValidator(m_guiValue._editorActualWidth);
                    break;
                case 1:
                    DrawRules();
                    break;
                case 2:
                    DrawConditions();
                    break;
                case 3:
                    DrawSettings(m_guiValue._editorActualWidth);
                    break;
                default:
                    DrawValidator(m_guiValue._editorActualWidth);
                    break;
            }
        }

        //==========================================================================================
        private static void ControlProject()
        {
            //ConsoleDebugLog("[TOSS VALIDATOR] --> ControlProject()");

            InitializeValidator();

            var executeTimer = new Stopwatch();

            executeTimer.Start();
            //--- START CONTROL --------------------------------------------------------------------

            ResetErrorData();

            if (m_filter._controlSpecialFolders && IsPossibilityDisplayErrors())
            {
                ControlSpecialFolders();
            }

            if (m_filter._controlFolders && IsPossibilityDisplayErrors())
            {
                ControlFolders();
            }

            if (m_filter._controlPrefabs && IsPossibilityDisplayErrors())
            {
                ControlPrefabs();
            }

            if (m_filter._controlScripts && IsPossibilityDisplayErrors())
            {
                ControlScripts();
            }

            if (m_filter._controlTextures && IsPossibilityDisplayErrors())
            {
                ControlTextures();
            }

            if (m_filter._controlScenes && IsPossibilityDisplayErrors())
            {
                ControlScenes();
            }

            if (m_filter._controlGraphics3D && IsPossibilityDisplayErrors())
            {
                ControlModels();
            }

            if (m_filter._controlSounds && IsPossibilityDisplayErrors())
            {
                ControlSounds();
            }

            if (m_filter._controlMaterials && IsPossibilityDisplayErrors())
            {
                ControlMaterials();
            }

            if (m_filter._controlAnimations && IsPossibilityDisplayErrors())
            {
                ControlAnimations();
            }

            //--- STOP CONTROL ---------------------------------------------------------------------
            executeTimer.Stop();
            m_controlTime = executeTimer.ElapsedMilliseconds;
        }

        //==========================================================================================
        private void OnDestroy()
        {
            SaveValidatorStates();
        }

        #region === CONTROL METHODS ================================================================

        //==========================================================================================
        private static void ControlSpecialFolders()
        {
            InitializeValidator();

            if (m_rules == null) return;

            var specialFolders = m_rules._specialFolders;
            var specialFoldersLength = specialFolders.Count;

            for (var i = 0; i < specialFoldersLength; i++)
            {
                if (specialFolders[i].Length == 0) continue;

                var pathSpecialFolder = GetCompleteAssetPath(specialFolders[i]);

                if (AssetDatabase.IsValidFolder(pathSpecialFolder)) continue;

                var splitFolderPath = GetSplitAssetPath(pathSpecialFolder);
                var folderName = splitFolderPath[splitFolderPath.Length - 1];
                var folderPath = GetPath(splitFolderPath);

                SetErrorData(m_icon._errorSpecialFolder,
                    ErrorType.SpecialFolderNotExists,
                    AssetType.Folder,
                    folderName,
                    folderPath,
                    string.Empty);
            }
        }

        //==========================================================================================
        private static void ControlFolders()
        {
            InitializeValidator();

            if (m_rules == null) return;

            var appPath = Application.dataPath;
            var indexPatternOfNaming = m_rules._patternFolders;
            var patternOfNaming = GetPatternOfNaming(indexPatternOfNaming, AssetType.Folder);
            var regexPatternOfNaming = new Regex(patternOfNaming);
            var folders = Directory.GetDirectories(appPath, "*", SearchOption.AllDirectories);

            #region === CONTROL NAMING =============================================================

            for (var i = 0; i < folders.Length; i++)
            {
                var splitFolderPath = GetSplitAssetPath(folders[i]);
                var folderName = splitFolderPath[splitFolderPath.Length - 1];

                if (splitFolderPath[0].Equals(UNITY_PACKAGES)) continue;

                if (IsInIgnoreFolder(splitFolderPath)) continue;

                if (regexPatternOfNaming.IsMatch(folderName)) continue;

                var folderPath = GetPath(splitFolderPath);

                SetErrorData(m_icon._errorWrongName,
                    ErrorType.FolderNameError,
                    AssetType.Folder,
                    folderName,
                    folderPath,
                    NamingPatterns[indexPatternOfNaming]);
            }

            #endregion
        }

        //==========================================================================================
        private static void ControlPrefabs()
        {
            InitializeValidator();

            if (m_rules == null) return;

            const string FILTER_PREFAB = "t:Prefab";

            var conditionPaths = ControlConditions(AssetType.Prefab,
                FILTER_PREFAB,
                Array.IndexOf(ConditionsTypes, "Prefab"));

            ControlLocationAndNaming(AssetType.Prefab,
                FILTER_PREFAB,
                m_rules,
                conditionPaths,
                ErrorType.PrefabLocationError,
                ErrorType.PrefabNameError);

            conditionPaths.Clear();
        }

        //==========================================================================================
        private static void ControlScripts()
        {
            InitializeValidator();

            if (m_rules == null) return;

            const string FILTER_SCRIPT = "t:Script";

            var conditionPaths = ControlConditions(AssetType.Script,
                FILTER_SCRIPT,
                Array.IndexOf(ConditionsTypes, "Script"));

            ControlLocationAndNaming(AssetType.Script,
                FILTER_SCRIPT,
                m_rules,
                conditionPaths,
                ErrorType.ScriptLocationError,
                ErrorType.ScriptNameError);

            conditionPaths.Clear();
        }

        //==========================================================================================
        private static void ControlTextures()
        {
            InitializeValidator();

            if (m_rules == null) return;

            const string FILTER_TEXTURE = "t:Texture";

            var conditionPaths = ControlConditions(AssetType.Texture,
                FILTER_TEXTURE,
                Array.IndexOf(ConditionsTypes, "Texture"));

            ControlLocationAndNaming(AssetType.Texture,
                FILTER_TEXTURE,
                m_rules,
                conditionPaths,
                ErrorType.TextureLocationError,
                ErrorType.TextureNameError);

            conditionPaths.Clear();
        }

        //==========================================================================================
        private static void ControlScenes()
        {
            InitializeValidator();

            if (m_rules == null) return;

            const string FILTER_SCENE = "t:Scene";

            var conditionPaths = ControlConditions(AssetType.Scene,
                FILTER_SCENE,
                Array.IndexOf(ConditionsTypes, "Scene"));

            ControlLocationAndNaming(AssetType.Scene,
                FILTER_SCENE,
                m_rules,
                conditionPaths,
                ErrorType.SceneLocationError,
                ErrorType.SceneNameError);

            conditionPaths.Clear();
        }

        //==========================================================================================
        private static void ControlSounds()
        {
            InitializeValidator();

            if (m_rules == null) return;

            const string FILTER_AUDIO_CLIP = "t:AudioClip";

            var conditionPaths = ControlConditions(AssetType.Sound,
                FILTER_AUDIO_CLIP,
                Array.IndexOf(ConditionsTypes, "Sound"));

            ControlLocationAndNaming(AssetType.Sound,
                FILTER_AUDIO_CLIP,
                m_rules,
                conditionPaths,
                ErrorType.SoundLocationError,
                ErrorType.SoundNameError);

            conditionPaths.Clear();
        }

        //==========================================================================================
        private static void ControlModels()
        {
            InitializeValidator();

            if (m_rules == null) return;

            const string FILTER_MODEL = "t:Model";

            var conditionPaths = ControlConditions(AssetType.Model,
                FILTER_MODEL,
                Array.IndexOf(ConditionsTypes, "Graphics3D"));

            ControlLocationAndNaming(AssetType.Model,
                FILTER_MODEL,
                m_rules,
                conditionPaths,
                ErrorType.ModelLocationError,
                ErrorType.ModelNameError);

            conditionPaths.Clear();
        }

        //==========================================================================================
        private static void ControlMaterials()
        {
            InitializeValidator();

            if (m_rules == null) return;

            const string FILTER_MATERIAL = "t:Material,t:Shader";

            var conditionPaths = ControlConditions(AssetType.Material,
                FILTER_MATERIAL,
                Array.IndexOf(ConditionsTypes, "Material"));

            ControlLocationAndNaming(AssetType.Material,
                FILTER_MATERIAL,
                m_rules,
                conditionPaths,
                ErrorType.MaterialLocationError,
                ErrorType.MaterialNameError);

            conditionPaths.Clear();
        }

        //==========================================================================================
        private static void ControlAnimations()
        {
            InitializeValidator();

            if (m_rules == null) return;

            const string FILTER_ANIMATION = "t:AnimationClip,t:AnimatorController";

            var conditionPaths = ControlConditions(AssetType.Animation,
                FILTER_ANIMATION,
                Array.IndexOf(ConditionsTypes, "Animation"));

            ControlLocationAndNaming(AssetType.Animation,
                FILTER_ANIMATION,
                m_rules,
                conditionPaths,
                ErrorType.AnimationLocationError,
                ErrorType.AnimationNameError);

            conditionPaths.Clear();
        }

        //==========================================================================================
        private static List<string> ControlConditions(AssetType assetType,
            string filter,
            int conditionAssetIndex)
        {
            var conditionPaths = new List<string>();

            if (!m_filter._controlConditions) return conditionPaths;

            var isAssetTypeInCondition = new bool[12];

            for (var i = 0; i < m_rules._conditionFormula.Count; i++)
            {
                // Int to Bool array
                for (var j = 0; j < isAssetTypeInCondition.Length; j++)
                {
                    isAssetTypeInCondition[j] = ((m_rules._conditionSelection[i] >> j) & 1) == 1;
                }

                if (!isAssetTypeInCondition[conditionAssetIndex]) continue;

                var splitActualCondition = GetSplitCondition(m_rules._conditionFormula[i]);

                if (splitActualCondition.Length != 1
                    && splitActualCondition.Length != 2) continue;

                if (splitActualCondition.Length == 2)
                {
                    var countSpecialChar = m_rules._conditionFormula[i].Split('*').Length - 1;
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
                            SetErrorData(m_icon._errorNotContain,
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
                                    SetErrorData(m_icon._errorNotContain,
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
                                SetErrorData(m_icon._errorNotValid,
                                    ErrorType.FolderNotValid,
                                    AssetType.Folder,
                                    GetMessageConditionRow(i, m_rules._conditionFormula[i]),
                                    parentFolderPath.ModifyPathSeparators(),
                                    string.Empty);
                            }
                        }
                    }
                }
                else
                {
                    SetErrorData(m_icon._errorNotValid,
                        ErrorType.FolderNotValid,
                        AssetType.Folder,
                        GetMessageConditionRow(i, m_rules._conditionFormula[i]),
                        pathSectionOne,
                        string.Empty);
                }
            }

            return conditionPaths;
        }

        //==========================================================================================
        private static void ControlLocationAndNaming(AssetType assetType,
            string filter,
            DValidatorRules rules,
            List<string> conditionPaths,
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
                    SetErrorData(m_icon._errorWrongLocation,
                        locationError,
                        assetType,
                        assetName,
                        folderPath,
                        GetLocationErrorMessage(m_rules._rootFolderTextures));
                }

                //--- Control Name -----------------------------------------------------------------
                if (regexPatternOfNaming.IsMatch(assetName)) continue;

                SetErrorData(m_icon._errorWrongName,
                    nameError,
                    assetType,
                    assetName,
                    folderPath,
                    NamingPatterns[indexPatternOfNaming]);
            }

            conditionPaths.Clear();
        }

        #endregion

        #region === DRAW METHODS ===================================================================

        //==========================================================================================
        [MenuItem("Tools/Toss Validator")]
        private static void UnityMenuTossValidator()
        {
            InitializeValidator();
            DrawMainWindow();
        }

        //==========================================================================================
        private static void DrawMainWindow()
        {
            m_mainWindow = (TossValidator) GetWindow(typeof(TossValidator));
            m_mainWindow.minSize = new Vector2(1024, 400);
            m_mainWindow.titleContent.image = m_icon._editorWindow;
            m_mainWindow.titleContent.text = "Toss Validator";
            m_mainWindow.titleContent.tooltip = "Toss Validator for Unity projects";
            m_mainWindow.Show();
        }

        //==========================================================================================
        private static void DrawValidator(float windowWidth)
        {
            GUILayout.Space(50f);

            #region === FILTER PANEL ===============================================================

            m_guiValue._styleDynamicMarginLeft.margin.top = 0;
            m_guiValue._styleDynamicMarginLeft.margin.left = (int) ((windowWidth - 960) / 2);

            EditorGUILayout.BeginHorizontal(m_guiValue._styleDynamicMarginLeft,
                GUILayout.Width(840), GUILayout.Height(40));

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox,
                GUILayout.Width(800), GUILayout.Height(40));

            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling);

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginVertical();
            m_filter._controlSpecialFolders = EditorGUILayout.ToggleLeft("Special Folders",
                m_filter._controlSpecialFolders,
                GUILayout.Width(120),
                GUILayout.Height(20));

            m_filter._controlScenes = EditorGUILayout.ToggleLeft("Scenes",
                m_filter._controlScenes,
                GUILayout.Width(120),
                GUILayout.Height(20));
            EditorGUILayout.EndVertical();

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginVertical();
            m_filter._controlFolders = EditorGUILayout.ToggleLeft("All Folders",
                m_filter._controlFolders,
                GUILayout.Width(120),
                GUILayout.Height(20));

            m_filter._controlGraphics3D = EditorGUILayout.ToggleLeft("3D (Models)",
                m_filter._controlGraphics3D,
                GUILayout.Width(120),
                GUILayout.Height(20));
            EditorGUILayout.EndVertical();

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginVertical();
            m_filter._controlPrefabs = EditorGUILayout.ToggleLeft("Prefabs",
                m_filter._controlPrefabs,
                GUILayout.Width(100),
                GUILayout.Height(20));

            m_filter._controlSounds = EditorGUILayout.ToggleLeft("Sounds",
                m_filter._controlSounds,
                GUILayout.Width(100),
                GUILayout.Height(20));
            EditorGUILayout.EndVertical();

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginVertical();
            m_filter._controlScripts = EditorGUILayout.ToggleLeft("Scripts",
                m_filter._controlScripts,
                GUILayout.Width(100),
                GUILayout.Height(20));

            m_filter._controlTextures = EditorGUILayout.ToggleLeft("Textures",
                m_filter._controlTextures,
                GUILayout.Width(100),
                GUILayout.Height(20));

            EditorGUILayout.EndVertical();

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginVertical();
            m_filter._controlMaterials = EditorGUILayout.ToggleLeft("Materials/Shaders",
                m_filter._controlMaterials,
                GUILayout.Width(160),
                GUILayout.Height(20));

            m_filter._controlAnimations = EditorGUILayout.ToggleLeft("Animations/Controllers",
                m_filter._controlAnimations,
                GUILayout.Width(160),
                GUILayout.Height(20));
            EditorGUILayout.EndVertical();

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginVertical();
            m_filter._controlConditions = EditorGUILayout.ToggleLeft("Conditions",
                m_filter._controlConditions,
                GUILayout.Width(120),
                GUILayout.Height(18));
            EditorGUILayout.EndVertical();

            // Button All - select all          
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button(new GUIContent("All", "Select All"),
                GUILayout.Width(50),
                GUILayout.Height(18)))
            {
                m_filter._controlSpecialFolders = true;
                m_filter._controlFolders = true;
                m_filter._controlPrefabs = true;
                m_filter._controlScripts = true;
                m_filter._controlTextures = true;
                m_filter._controlScenes = true;
                m_filter._controlGraphics3D = true;
                m_filter._controlSounds = true;
                m_filter._controlMaterials = true;
                m_filter._controlAnimations = true;

                m_filter._controlConditions = true;
            }

            // Button None - deselect all
            if (GUILayout.Button(new GUIContent("None", "Deselect All"),
                GUILayout.Width(50),
                GUILayout.Height(19)))
            {
                m_filter._controlSpecialFolders = false;
                m_filter._controlFolders = false;
                m_filter._controlPrefabs = false;
                m_filter._controlScripts = false;
                m_filter._controlTextures = false;
                m_filter._controlScenes = false;
                m_filter._controlGraphics3D = false;
                m_filter._controlSounds = false;
                m_filter._controlMaterials = false;
                m_filter._controlAnimations = false;

                m_filter._controlConditions = false;
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

            #region === MIDDLE PANEL ===============================================================

            m_guiValue._styleDynamicMarginLeft.margin.top = 0;
            m_guiValue._styleDynamicMarginLeft.margin.left = (int) ((windowWidth - 960) / 2);

            EditorGUILayout.BeginHorizontal(m_guiValue._styleDynamicMarginLeft,
                GUILayout.Width(960));

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginHorizontal(GUILayout.Width(304));
            EditorGUILayout.HelpBox("TIME OF CONTROL: " + m_controlTime + " ms",
                MessageType.Info,
                true);
            EditorGUILayout.EndHorizontal();

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginHorizontal(GUILayout.Width(340));
            if (m_countErrors == 0)
            {
                EditorGUILayout.HelpBox("ERRORS IN ASSETS FOLDER: 0",
                    MessageType.Info,
                    true);
            }
            else
            {
                string errorMessage;
                if (m_countErrors <= m_settings._countDisplayErrors)
                {
                    errorMessage = "ERRORS IN ASSETS FOLDER: " + m_countErrors;
                    EditorGUILayout.HelpBox(errorMessage, MessageType.Error, true);
                }
                else
                {
                    errorMessage = "ERRORS IN ASSETS FOLDER: " + m_settings._countDisplayErrors +
                                   "+";
                    EditorGUILayout.HelpBox(errorMessage, MessageType.Error, true);
                }
            }

            EditorGUILayout.EndHorizontal();

            //--------------------------------------------------------------------------------------
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Width(304));
            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling || Errors.Count == 0);

            EditorGUILayout.BeginHorizontal(m_guiValue._styleExportButtonMarginLeft,
                GUILayout.Width(200),
                GUILayout.Height(26));

            if (GUILayout.Button(
                new GUIContent("Export results to file", "Export results (errors) to CSV file"),
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

            #region === RESULTS / ROWS =============================================================

            // Horizontal line / Separator
            EditorGUI.DrawRect(new Rect(0, 160, windowWidth, 1), Color.black);

            m_guiValue._scrollPosValidator =
                EditorGUILayout.BeginScrollView(m_guiValue._scrollPosValidator);

            if (Errors.Count == 0)
            {
                m_guiValue._styleDynamicMarginLeft.margin.top = 50;
                m_guiValue._styleDynamicMarginLeft.margin.left = (int) ((windowWidth - 340) / 2);

                EditorGUILayout.BeginHorizontal(m_guiValue._styleDynamicMarginLeft,
                    GUILayout.Width(340));

                EditorGUILayout.HelpBox(GetWarningMessageAfterCompile(),
                    MessageType.Info,
                    true);

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                var loopLength = m_countErrors <= m_settings._countDisplayErrors
                    ? m_countErrors
                    : m_settings._countDisplayErrors;

                for (var i = 0; i < loopLength; i++)
                {
                    DrawRow(i);
                }
            }

            EditorGUILayout.EndScrollView();

            #endregion

            DrawBottomPanel();
        }

        //==========================================================================================
        private static void DrawRules()
        {
            GUILayout.Space(50f);

            m_guiValue._scrollPosRules =
                EditorGUILayout.BeginScrollView(m_guiValue._scrollPosRules);

            #region === PATTERNS ===================================================================

            m_guiValue._foldoutPatterns = EditorGUILayout.Foldout(
                m_guiValue._foldoutPatterns,
                m_guiValue._titlePatterns,
                true);

            if (m_guiValue._foldoutPatterns)
            {
                EditorGUILayout.BeginHorizontal();

                //----------------------------------------------------------------------------------
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                m_rules._patternFolders = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Folder),
                    m_rules._patternFolders,
                    NamingPatterns);

                m_rules._patternPrefabs = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Prefab),
                    m_rules._patternPrefabs,
                    NamingPatterns);

                EditorGUILayout.LabelField(GetFormattedLabelAssetType(AssetType.Script),
                    NamingPatterns[0]);

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();

                //----------------------------------------------------------------------------------
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                m_rules._patternTextures = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Texture),
                    m_rules._patternTextures,
                    NamingPatterns);

                m_rules._patternScenes = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Scene),
                    m_rules._patternScenes,
                    NamingPatterns);

                m_rules._patternGraphics3D = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Model),
                    m_rules._patternGraphics3D,
                    NamingPatterns);

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();

                //----------------------------------------------------------------------------------
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                m_rules._patternSounds = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Sound),
                    m_rules._patternSounds,
                    NamingPatterns);

                m_rules._patternMaterials = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Material),
                    m_rules._patternMaterials,
                    NamingPatterns);

                m_rules._patternAnimations = EditorGUILayout.Popup(
                    GetFormattedLabelAssetType(AssetType.Animation),
                    m_rules._patternAnimations,
                    NamingPatterns);

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }

            #endregion

            GUILayout.Space(20f);

            #region === MAIN ROOT FOLDERS ==========================================================

            m_guiValue._foldoutRootFolders = EditorGUILayout.Foldout(
                m_guiValue._foldoutRootFolders,
                m_guiValue._titleRootFolders,
                true,
                EditorStyles.foldout);

            if (m_guiValue._foldoutRootFolders)
            {
                EditorGUILayout.BeginHorizontal();

                //----------------------------------------------------------------------------------
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                m_rules._rootFolderPrefabs =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Prefab),
                        m_rules._rootFolderPrefabs);

                m_rules._rootFolderScripts =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Script),
                        m_rules._rootFolderScripts);

                m_rules._rootFolderTextures =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Texture),
                        m_rules._rootFolderTextures);

                m_rules._rootFolderScenes =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Scene),
                        m_rules._rootFolderScenes);

                EditorGUILayout.LabelField(" ", GetInfoTextRootFolders());

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();

                //----------------------------------------------------------------------------------
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                m_rules._rootFolderGraphics3D =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Model),
                        m_rules._rootFolderGraphics3D);

                m_rules._rootFolderSounds =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Sound),
                        m_rules._rootFolderSounds);

                m_rules._rootFolderMaterials =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Material),
                        m_rules._rootFolderMaterials);

                m_rules._rootFolderAnimations =
                    EditorGUILayout.TextField(GetFormattedLabelAssetType(AssetType.Animation),
                        m_rules._rootFolderAnimations);

                EditorGUILayout.LabelField(" ", GetInfoTextRootFolders());

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }

            #endregion

            GUILayout.Space(20f);

            #region === SPECIAL FOLDERS ============================================================

            m_guiValue._foldoutSpecialFolders = EditorGUILayout.Foldout(
                m_guiValue._foldoutSpecialFolders,
                m_guiValue._titleSpecialFolders,
                true);

            if (m_guiValue._foldoutSpecialFolders)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                // Add button
                GUI.color = m_guiValue._addButtonColor;
                if (GUILayout.Button("Add Special Folder", GUILayout.Height(24)))
                {
                    if (m_rules._specialFolders.Count < m_settings._countSpecialFolders)
                    {
                        m_rules._specialFolders.Add(string.Empty);
                    }
                }

                GUI.color = Color.white;

                // List of Special Folders
                for (var i = 0; i < m_rules._specialFolders.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        m_rules._specialFolders[i] =
                            EditorGUILayout.TextField(m_guiValue._labelSpecialFolder,
                                m_rules._specialFolders[i]);

                        GUI.color = m_guiValue._deleteButtonColor;
                        if (GUILayout.Button(new GUIContent("X", "Close"), GUILayout.Width(22)))
                        {
                            m_rules._specialFolders.RemoveAt(i);
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

            #region === IGNORE FOLDERS =============================================================

            m_guiValue._foldoutIgnoreFolders = EditorGUILayout.Foldout(
                m_guiValue._foldoutIgnoreFolders,
                m_guiValue._titleIgnoreFolders,
                true);

            if (m_guiValue._foldoutIgnoreFolders)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                // Add button
                GUI.color = m_guiValue._addButtonColor;
                if (GUILayout.Button("Add Ignore Folder", GUILayout.Height(24)))
                {
                    if (m_rules._ignoreFolders.Count < m_settings._countIgnoreFolders)
                        m_rules._ignoreFolders.Add(string.Empty);
                }

                GUI.color = Color.white;

                // List of ignore folders
                for (var i = 0; i < m_rules._ignoreFolders.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        m_rules._ignoreFolders[i] =
                            EditorGUILayout.TextField(m_guiValue._labelIgnoreFolder,
                                m_rules._ignoreFolders[i]);

                        GUI.color = m_guiValue._deleteButtonColor;
                        if (GUILayout.Button(new GUIContent("X", "Close"), GUILayout.Width(22)))
                        {
                            m_rules._ignoreFolders.RemoveAt(i);
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

            EditorUtility.SetDirty(m_rules);
        }

        //==========================================================================================
        private static void DrawConditions()
        {
            GUILayout.Space(50f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space();

            // Add button
            GUI.color = m_guiValue._addButtonColor;
            if (GUILayout.Button("Add Condition", GUILayout.Height(24)))
            {
                if (m_rules._conditionFormula.Count < m_settings._countConditionRows)
                {
                    m_rules._conditionFormula.Add(string.Empty);
                    m_rules._conditionSelection.Add(0);
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

        //==========================================================================================
        private static void DrawConditionRow()
        {
            for (var i = 0; i < m_rules._conditionFormula.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var countSpecialChar = m_rules._conditionFormula[i].Split('*').Length - 1;

                if (countSpecialChar == 0 || countSpecialChar == 1)
                {
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = Color.red;
                }

                m_rules._conditionFormula[i] = EditorGUILayout.TextField(
                    GetFormattedLabelCondition(i),
                    m_rules._conditionFormula[i]);
                GUI.color = Color.white;

                m_rules._conditionSelection[i] = EditorGUILayout.MaskField(
                    m_rules._conditionSelection[i],
                    ConditionsTypes,
                    GUILayout.Width(150));

                //--- Button move row up -----------------------------------------------------------
                if (GUILayout.Button(m_icon._arrowUp, GUILayout.Width(22)))
                {
                    if (i != 0)
                    {
                        var exchangeHelper1 = m_rules._conditionFormula[i];
                        var exchangeHelper2 = m_rules._conditionSelection[i];

                        m_rules._conditionFormula[i] = m_rules._conditionFormula[i - 1];
                        m_rules._conditionSelection[i] = m_rules._conditionSelection[i - 1];

                        m_rules._conditionFormula[i - 1] = exchangeHelper1;
                        m_rules._conditionSelection[i - 1] = exchangeHelper2;
                    }
                }

                //--- Button move row down ---------------------------------------------------------
                if (GUILayout.Button(m_icon._arrowDown, GUILayout.Width(22)))
                {
                    if (i != m_rules._conditionFormula.Count - 1)
                    {
                        var exchangeHelper1 = m_rules._conditionFormula[i];
                        var exchangeHelper2 = m_rules._conditionSelection[i];

                        m_rules._conditionFormula[i] = m_rules._conditionFormula[i + 1];
                        m_rules._conditionSelection[i] = m_rules._conditionSelection[i + 1];

                        m_rules._conditionFormula[i + 1] = exchangeHelper1;
                        m_rules._conditionSelection[i + 1] = exchangeHelper2;
                    }
                }

                GUI.color = m_guiValue._duplicateButtonColor;

                //--- Button duplicate row ---------------------------------------------------------
                if (GUILayout.Button(new GUIContent("D", "Duplicate row"), GUILayout.Width(22)))
                {
                    if (m_rules._conditionFormula.Count < m_settings._countConditionRows)
                    {
                        m_rules._conditionFormula.Insert(i, m_rules._conditionFormula[i]);
                        m_rules._conditionSelection.Insert(i, m_rules._conditionSelection[i]);
                    }
                }

                GUI.color = m_guiValue._deleteButtonColor;

                //--- Button delete row ------------------------------------------------------------
                if (GUILayout.Button(new GUIContent("X", "Delete row"), GUILayout.Width(22)))
                {
                    m_rules._conditionFormula.RemoveAt(i);
                    m_rules._conditionSelection.RemoveAt(i);
                }

                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();
            }
        }

        //==========================================================================================
        private static void DrawSettings(float windowWidth)
        {
            GUILayout.Space(50f);

            m_guiValue._styleDynamicMarginLeft.margin.top = 0;
            m_guiValue._styleDynamicMarginLeft.margin.left = (int) ((windowWidth - 600) / 2);

            EditorGUILayout.BeginHorizontal(m_guiValue._styleDynamicMarginLeft);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(600));
            EditorGUILayout.Space();

            m_settings._countDisplayErrors = EditorGUILayout.IntSlider("Count Display Errors:",
                m_settings._countDisplayErrors,
                10,
                MAX_COUNT_DISPLAY_ERRORS);

            EditorGUILayout.Space();

            m_settings._countSpecialFolders = EditorGUILayout.IntSlider("Count Special Folders:",
                m_settings._countSpecialFolders,
                10,
                MAX_COUNT_SPECIAL_FOLDERS);

            EditorGUILayout.Space();

            m_settings._countIgnoreFolders = EditorGUILayout.IntSlider("Count Ignore Folders:",
                m_settings._countIgnoreFolders,
                10,
                MAX_COUNT_IGNORE_FOLDERS);

            EditorGUILayout.Space();

            m_settings._countConditionRows = EditorGUILayout.IntSlider("Count Condition Rows:",
                m_settings._countConditionRows,
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

        //==========================================================================================
        private static void DrawRow(int index)
        {
            var assetName = Errors[index]._assetName;
            var assetPath = Errors[index]._assetPath;
            var errorType = Errors[index]._errorType;
            var errorName = Errors[index]._errorName;
            var assetType = Errors[index]._assetType;
            var correctPattern = Errors[index]._correctPattern;

            var rowStyle = index % 2 == 0 ? m_guiValue._styleEvenRow : m_guiValue._styleOddRow;
            EditorGUILayout.BeginHorizontal(rowStyle, GUILayout.Height(50));

            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling);

            if (errorType == ErrorType.SpecialFolderNotExists
                || errorType == ErrorType.FolderNotValid)
            {
                GUILayout.Box(Errors[index]._errorIcon,
                    rowStyle,
                    GUILayout.Width(50),
                    GUILayout.Height(50));
            }
            else
            {
                // Left Row Button
                if (GUILayout.Button(Errors[index]._errorIcon,
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

                            var path = GetFormattedAssetPath(assetPath, assetName);
                            SelectAsset(path);

                            break;

                        case ErrorType.FolderNotContain:

                            SelectAsset(assetPath);

                            break;

                        case ErrorType.SpecialFolderNotExists:
                        case ErrorType.FolderNotValid:

                            // No OnClick Functionality

                            break;
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

        //==========================================================================================
        private static void DrawBottomPanel()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.Label("Created by: Toss");
                GUILayout.FlexibleSpace();
                GUILayout.Label("Version: 1.5");
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region === WORK / HELP METHODS ============================================================

        //==========================================================================================
        private static void InitializeValidator()
        {
            if (m_rules != null) return;

            //ConsoleDebugLog("[TOSS VALIDATOR] --> InitializeValidator()");

            InitializeProjectRules();

            LoadValidatorStates();
        }

        //==========================================================================================
        private static void InitializeProjectRules()
        {
            var guidValidatorSettings = AssetDatabase.FindAssets("ValidatorSettings");

            if (guidValidatorSettings.Length == 0) return;

            var pathValidatorSettings = AssetDatabase.GUIDToAssetPath(guidValidatorSettings[0]);

            var pathImages = pathValidatorSettings.Replace("ValidatorSettings.asset", "Images/");

            InitializeIconsAndStyles(pathImages);

            m_rules = (DValidatorRules) AssetDatabase.LoadAssetAtPath(
                pathValidatorSettings,
                typeof(DValidatorRules));

            //m_rules = (DValidatorRules) EditorGUIUtility.Load(pathValidatorSettings);
        }

        //==========================================================================================
        private static void InitializeIconsAndStyles(string pathImages)
        {
            m_icon._editorWindow = GetTexture2D(pathImages, "icon-editor-window");
            m_icon._errorSpecialFolder = GetTexture2D(pathImages, "icon-error-folder-special");
            m_icon._errorWrongLocation = GetTexture2D(pathImages, "icon-error-wrong-location");
            m_icon._errorWrongName = GetTexture2D(pathImages, "icon-error-wrong-name");
            m_icon._errorNotContain = GetTexture2D(pathImages, "icon-error-not-contain");
            m_icon._errorNotValid = GetTexture2D(pathImages, "icon-error-not-valid");
            m_icon._arrowUp = GetTexture2D(pathImages, "arrow-up");
            m_icon._arrowDown = GetTexture2D(pathImages, "arrow-down");

            var styleEvenRow = m_guiValue._styleEvenRow;

            styleEvenRow.normal.background = EditorGUIUtility.isProSkin
                ? GetTexture2D(pathImages, "even-row-bg-dark")
                : GetTexture2D(pathImages, "even-row-bg-light");

            styleEvenRow.padding = new RectOffset(10, 6, 6, 6);

            m_guiValue._styleOddRow.padding = new RectOffset(10, 6, 6, 6);
            m_guiValue._styleExportButtonMarginLeft.margin.left = 48;
        }

        //==========================================================================================
        private static void SaveValidatorStates()
        {
            AssetDatabase.SaveAssets();
            SaveFilter();
            SaveSettings();
        }

        //==========================================================================================
        private static void SaveFilter()
        {
            EditorPrefs.SetBool("controlSpecialFolders", m_filter._controlSpecialFolders);
            EditorPrefs.SetBool("controlFolders", m_filter._controlFolders);
            EditorPrefs.SetBool("controlPrefabs", m_filter._controlPrefabs);
            EditorPrefs.SetBool("controlScripts", m_filter._controlScripts);
            EditorPrefs.SetBool("controlTextures", m_filter._controlTextures);
            EditorPrefs.SetBool("controlScenes", m_filter._controlScenes);
            EditorPrefs.SetBool("controlGraphics3D", m_filter._controlGraphics3D);
            EditorPrefs.SetBool("controlSounds", m_filter._controlSounds);
            EditorPrefs.SetBool("controlMaterials", m_filter._controlMaterials);
            EditorPrefs.SetBool("controlAnimations", m_filter._controlAnimations);

            EditorPrefs.SetBool("controlConditions", m_filter._controlConditions);
        }

        //==========================================================================================
        private static void SaveSettings()
        {
            EditorPrefs.SetInt("countDisplayErrors", m_settings._countDisplayErrors);
            EditorPrefs.SetInt("countSpecialFolders", m_settings._countSpecialFolders);
            EditorPrefs.SetInt("countIgnoreFolders", m_settings._countIgnoreFolders);
            EditorPrefs.SetInt("countConditionRows", m_settings._countConditionRows);
        }

        //==========================================================================================
        private static void LoadValidatorStates()
        {
            LoadFilter();
            LoadSettings();
        }

        //==========================================================================================
        private static void LoadFilter()
        {
            m_filter._controlSpecialFolders = EditorPrefs.GetBool("controlSpecialFolders", true);
            m_filter._controlFolders = EditorPrefs.GetBool("controlFolders", true);
            m_filter._controlPrefabs = EditorPrefs.GetBool("controlPrefabs", true);
            m_filter._controlScripts = EditorPrefs.GetBool("controlScripts", true);
            m_filter._controlTextures = EditorPrefs.GetBool("controlTextures", true);
            m_filter._controlScenes = EditorPrefs.GetBool("controlScenes", true);
            m_filter._controlGraphics3D = EditorPrefs.GetBool("controlGraphics3D", true);
            m_filter._controlSounds = EditorPrefs.GetBool("controlSounds", true);
            m_filter._controlMaterials = EditorPrefs.GetBool("controlMaterials", true);
            m_filter._controlAnimations = EditorPrefs.GetBool("controlAnimations", true);

            m_filter._controlConditions = EditorPrefs.GetBool("controlConditions", true);
        }

        //==========================================================================================
        private static void LoadSettings()
        {
            m_settings._countDisplayErrors = EditorPrefs.GetInt("countDisplayErrors", 40);
            m_settings._countSpecialFolders = EditorPrefs.GetInt("countSpecialFolders", 10);
            m_settings._countIgnoreFolders = EditorPrefs.GetInt("countIgnoreFolders", 10);
            m_settings._countConditionRows = EditorPrefs.GetInt("countConditionRows", 10);
        }

        //==========================================================================================
        private static void ExportErrorsToFile()
        {
            if (Errors.Count == 0) return;

            try
            {
                using (var file = new StreamWriter("TestFile2.csv", true))
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
                        sb.Append(Errors[i]._errorName);
                        sb.Append(FILE_SEPARATOR);
                        sb.Append(Errors[i]._assetType);
                        sb.Append(FILE_SEPARATOR);
                        sb.Append(Errors[i]._assetName);
                        sb.Append(FILE_SEPARATOR);
                        sb.Append(Errors[i]._assetPath);

                        file.WriteLine(sb);

                        sb.Clear();
                    }
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException("[TossValidator] Export file error: ", e);
            }
        }

        //==========================================================================================
        private static void SetErrorData(Texture2D errorIcon,
            ErrorType errorType,
            AssetType assetType,
            string assetName,
            string assetPath,
            string example)
        {
            if (!IsPossibilityDisplayErrors()) return;

            m_countErrors++;

            var errorName = GetErrorName(errorType);

            Errors.Add(new Error(errorIcon,
                errorType,
                assetType,
                errorName,
                assetName,
                assetPath,
                example));
        }

        //==========================================================================================
        private static bool IsPossibilityDisplayErrors()
        {
            return m_countErrors <= m_settings._countDisplayErrors;
        }

        //==========================================================================================
        private static void ResetErrorData()
        {
            m_countErrors = 0;
            Errors.Clear();
        }

        //==========================================================================================
        private static void SelectAsset(string assetPath)
        {
            var assetObject = AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEngine.Object));
            Selection.activeObject = assetObject;
        }

        //==========================================================================================
        private static string GetLocationErrorMessage(string nameRootFolder)
        {
            var sb = new StringBuilder();
            sb.Append("[Root folder(s): ");
            sb.Append(nameRootFolder);
            sb.Append("] or [Check Conditions]");
            return sb.ToString();
        }

        //==========================================================================================
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

        //==========================================================================================
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

        //==========================================================================================
        private static bool IsInIgnoreFolder(string[] input)
        {
            for (var i = 0; i < m_rules._ignoreFolders.Count; i++)
            {
                if (m_rules._ignoreFolders[i].Length == 0) continue;

                if (Array.IndexOf(input, m_rules._ignoreFolders[i]) > -1)
                    return true;
            }

            return false;
        }

        //==========================================================================================
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

        //==========================================================================================
        private static string GetCompleteAssetPath(string actualPath)
        {
            var sb = new StringBuilder();
            sb.Append("Assets");
            sb.Append(ASSET_PATH_SEPARATOR);
            sb.Append(actualPath);
            return sb.ToString();
        }

        //==========================================================================================
        private static string GetMessageMissingAsset(AssetType assetType)
        {
            var sb = new StringBuilder();
            sb.Append("[Missing ");
            sb.Append(assetType);
            sb.Append("]");
            return sb.ToString();
        }

        //==========================================================================================
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

        //==========================================================================================
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

        //==========================================================================================
        private static string GetFormattedLabel(string label)
        {
            var sb = new StringBuilder();
            sb.Append(' ', 8);
            sb.Append(label);
            return sb.ToString();
        }

        //==========================================================================================
        private static string GetFormattedLabelCondition(int rowIndex)
        {
            var sb = new StringBuilder();
            sb.Append(' ', 4);
            sb.Append("Condition ");
            sb.Append(rowIndex + 1);
            sb.Append(": Assets\\");
            return sb.ToString();
        }

        //==========================================================================================
        private static string GetFormattedAssetPath(string assetPath, string assetName)
        {
            var sb = new StringBuilder();
            sb.Append(assetPath);
            sb.Append(ASSET_PATH_SEPARATOR);
            sb.Append(assetName);
            return sb.ToString();
        }

        //==========================================================================================
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

        //==========================================================================================      
        private static string[] GetSplitAssetPath(string assetPath)
        {
            return assetPath.Split(new[] {"\\", "/"}, StringSplitOptions.RemoveEmptyEntries);
        }

        //==========================================================================================
        private static string[] GetSplitCondition(string condition)
        {
            return condition.Split(new[] {"\\*\\", "\\*"}, StringSplitOptions.RemoveEmptyEntries);
        }

        //==========================================================================================
        private static string GetParentFolderPath(string[] splitCondition, string pathSubFolder)
        {
            var sb = new StringBuilder();
            sb.Append(pathSubFolder);

            if (splitCondition.Length != 2) return sb.ToString();
            sb.Append("/");
            sb.Append(splitCondition[1]);

            return sb.ToString();
        }

        //==========================================================================================
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

        //==========================================================================================
        private static string GetInfoTextRootFolders()
        {
            return "[Separator: | (OR)]";
        }

        //==========================================================================================
        private static string GetInfoTextSpecialFolders()
        {
            var sb = new StringBuilder();
            sb.Append("[Separator: \\] [Max. count: ");
            sb.Append(m_settings._countSpecialFolders);
            sb.Append("] [Special Folder = folder that the project must contain");
            sb.Append(" (example: Documentation, Controllers, etc.)]");
            return sb.ToString();
        }

        //==========================================================================================
        private static string GetInfoTextIgnoreFolders()
        {
            var sb = new StringBuilder();
            sb.Append("[Max. count: ");
            sb.Append(m_settings._countIgnoreFolders);
            sb.Append("]");
            return sb.ToString();
        }

        //==========================================================================================
        private static string GetInfoTextConditions()
        {
            var sb = new StringBuilder();
            sb.Append("[All subfolders symbol: * (One per condition)] ");
            sb.Append("[Separator: \\] ");
            sb.Append("[Max. count: ");
            sb.Append(m_settings._countConditionRows);
            sb.Append("]");
            return sb.ToString();
        }

        //==========================================================================================
        private static Texture2D GetTexture2D(string texturePath, string textureName)
        {
            var sb = new StringBuilder();
            sb.Append(texturePath);
            sb.Append(textureName);
            sb.Append(".png");
            //return (Texture2D) AssetDatabase.LoadAssetAtPath(sb.ToString(), typeof(Texture2D));
            return (Texture2D) EditorGUIUtility.Load(sb.ToString());
        }

        //==========================================================================================
        private static void ControlActualStatesCompareToSettings()
        {
            if (m_rules._specialFolders.Count > m_settings._countSpecialFolders)
            {
                m_rules._specialFolders.RemoveRange(
                    m_settings._countSpecialFolders,
                    m_rules._specialFolders.Count - m_settings._countSpecialFolders);
            }

            if (m_rules._ignoreFolders.Count > m_settings._countIgnoreFolders)
            {
                m_rules._ignoreFolders.RemoveRange(
                    m_settings._countIgnoreFolders,
                    m_rules._ignoreFolders.Count - m_settings._countIgnoreFolders);
            }

            if (m_rules._conditionFormula.Count > m_settings._countConditionRows)
            {
                m_rules._conditionFormula.RemoveRange(
                    m_settings._countConditionRows,
                    m_rules._conditionFormula.Count - m_settings._countConditionRows);

                m_rules._conditionSelection.RemoveRange(
                    m_settings._countConditionRows,
                    m_rules._conditionSelection.Count - m_settings._countConditionRows);
            }
        }

        #endregion

        #region === STRUCTS ========================================================================

        //==========================================================================================
        private struct Error
        {
            public readonly Texture2D _errorIcon;
            public readonly ErrorType _errorType;
            public readonly AssetType _assetType;
            public readonly string _errorName;
            public readonly string _assetName;
            public readonly string _assetPath;
            public readonly string _correctPattern;

            public Error(Texture2D errorIcon,
                ErrorType errorType,
                AssetType assetType,
                string errorName,
                string assetName,
                string assetPath,
                string correctPattern)
            {
                _errorIcon = errorIcon;
                _errorType = errorType;
                _assetType = assetType;
                _errorName = errorName;
                _assetName = assetName;
                _assetPath = assetPath;
                _correctPattern = correctPattern;
            }
        }

        //==========================================================================================
        private struct Filter
        {
            public bool _controlSpecialFolders;
            public bool _controlFolders;
            public bool _controlPrefabs;
            public bool _controlScripts;
            public bool _controlTextures;
            public bool _controlScenes;
            public bool _controlGraphics3D;
            public bool _controlSounds;
            public bool _controlMaterials;
            public bool _controlAnimations;
            public bool _controlConditions;
        }

        //==========================================================================================
        private struct Icon
        {
            public Texture2D _editorWindow;
            public Texture2D _errorSpecialFolder;
            public Texture2D _errorWrongLocation;
            public Texture2D _errorWrongName;
            public Texture2D _errorNotContain;
            public Texture2D _errorNotValid;
            public Texture2D _arrowUp;
            public Texture2D _arrowDown;
        }

        //==========================================================================================
        private struct GuiValue
        {
            public float _editorActualWidth;
            public int _indexToolbarSelected;

            public bool _foldoutPatterns;
            public string _titlePatterns;
            public bool _foldoutRootFolders;
            public string _titleRootFolders;
            public bool _foldoutSpecialFolders;
            public string _titleSpecialFolders;
            public bool _foldoutIgnoreFolders;
            public string _titleIgnoreFolders;

            public string _labelSpecialFolder;
            public string _labelIgnoreFolder;

            public GUIStyle _styleEvenRow;
            public GUIStyle _styleOddRow;
            public GUIStyle _styleDynamicMarginLeft;
            public GUIStyle _styleExportButtonMarginLeft;

            public Vector2 _scrollPosValidator;
            public Vector2 _scrollPosRules;

            public Color _addButtonColor;
            public Color _deleteButtonColor;
            public Color _duplicateButtonColor;
        }

        //==========================================================================================
        private struct Settings
        {
            public int _countDisplayErrors;
            public int _countSpecialFolders;
            public int _countIgnoreFolders;
            public int _countConditionRows;
        }

        #endregion

        //==========================================================================================
        private static void ConsoleDebugLog(string message)
        {
            var sb = new StringBuilder();
            sb.Append("<b>");
            sb.Append("<color=#003399>");
            sb.Append(message);
            sb.Append("</color>");
            sb.Append("</b>");

            UnityEngine.Debug.Log(sb);
        }

        //==========================================================================================
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

        //==========================================================================================
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

    //==============================================================================================
    /// <summary>
    ///     EXTENSIONS
    /// </summary>
    public static class TossValidatorExtensions
    {
        //==========================================================================================
        public static string ModifyPathSeparators(this string input)
        {
            return input.Replace('/', '\\');
        }

        //==========================================================================================
        public static string[] SplitRootFolders(this string input)
        {
            return input.Split(new[] {"|"}, StringSplitOptions.RemoveEmptyEntries);
        }

        //==========================================================================================
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