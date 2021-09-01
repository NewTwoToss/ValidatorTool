// =================================================================================================
//     Author:			Tomas "SkyToss" Szilagyi
//     Date created:	05.04.2018
// =================================================================================================

using UnityEditor;
using UnityEngine;

namespace TossValidator
{
    [CustomEditor(typeof(DValidatorRules))]
    public class DValidatorRulesInspector : Editor
    {
        private DValidatorRules _src;

        private readonly string[] _patternsTypes =
        {
            "ExampleOfNaming",
            "exampleOfNaming",
            "example_Of_Naming",
            "example-Of-Naming",
            "Example_Of_Naming"
        };

        private void OnEnable()
        {
            _src = (DValidatorRules) target;
        }

        public override void OnInspectorGUI()
        {
            GUI.enabled = false;

            EditorGUILayout.LabelField("PATTERN OF NAMING", EditorStyles.boldLabel);
            _src._patternFolders =
                EditorGUILayout.Popup("Folders:", _src._patternFolders, _patternsTypes);
            _src._patternPrefabs =
                EditorGUILayout.Popup("Prefabs:", _src._patternPrefabs, _patternsTypes);
            _src._patternScripts =
                EditorGUILayout.Popup("Scripts:", _src._patternScripts, _patternsTypes);
            _src._patternTextures =
                EditorGUILayout.Popup("Textures:", _src._patternTextures, _patternsTypes);
            _src._patternScenes =
                EditorGUILayout.Popup("Scenes:", _src._patternScenes, _patternsTypes);
            _src._patternGraphics3D =
                EditorGUILayout.Popup("Graphics 3D:", _src._patternGraphics3D, _patternsTypes);
            _src._patternSounds =
                EditorGUILayout.Popup("Sounds:", _src._patternSounds, _patternsTypes);
            _src._patternMaterials =
                EditorGUILayout.Popup("Materials:", _src._patternMaterials, _patternsTypes);
            _src._patternAnimations =
                EditorGUILayout.Popup("Animations:", _src._patternAnimations, _patternsTypes);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("ROOT FOLDERS", EditorStyles.boldLabel);
            _src._rootFolderPrefabs =
                EditorGUILayout.TextField("Prefabs:", _src._rootFolderPrefabs);
            _src._rootFolderScripts =
                EditorGUILayout.TextField("Scripts:", _src._rootFolderScripts);
            _src._rootFolderTextures =
                EditorGUILayout.TextField("Textures:", _src._rootFolderTextures);
            _src._rootFolderScenes =
                EditorGUILayout.TextField("Scenes:", _src._rootFolderScenes);
            _src._rootFolderGraphics3D =
                EditorGUILayout.TextField("Graphics 3D:", _src._rootFolderGraphics3D);
            _src._rootFolderSounds =
                EditorGUILayout.TextField("Sounds:", _src._rootFolderSounds);
            _src._rootFolderMaterials =
                EditorGUILayout.TextField("Materials:", _src._rootFolderMaterials);
            _src._rootFolderAnimations =
                EditorGUILayout.TextField("Animations:", _src._rootFolderAnimations);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("SPECIAL FOLDERS", EditorStyles.boldLabel);
            for (var i = 0; i < _src._specialFolders.Count; i++)
            {
                EditorGUILayout.TextField("Folder name:", _src._specialFolders[i]);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("IGNORE FOLDERS", EditorStyles.boldLabel);
            for (var i = 0; i < _src._ignoreFolders.Count; i++)
            {
                EditorGUILayout.TextField("Folder name:", _src._ignoreFolders[i]);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("CONDITIONS", EditorStyles.boldLabel);
            for (var i = 0; i < _src._conditionFormula.Count; i++)
            {
                _src._conditionFormula[i] = EditorGUILayout.TextField("Name:",
                    _src._conditionFormula[i]);
                _src._conditionSelection[i] = EditorGUILayout.IntField("Selection:",
                    _src._conditionSelection[i]);
                EditorGUILayout.Space();
            }

            GUI.enabled = true;

            EditorGUILayout.Space();

            EditorUtility.SetDirty(_src);
        }
    }
}