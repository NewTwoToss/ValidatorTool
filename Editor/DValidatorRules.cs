using System.Collections.Generic;
using UnityEngine;

namespace TossValidator
{
    public class DValidatorRules : ScriptableObject
    {
        public int _patternFolders;
        public int _patternPrefabs;
        public int _patternScripts;
        public int _patternTextures;
        public int _patternScenes;
        public int _patternGraphics3D;
        public int _patternSounds;
        public int _patternMaterials;
        public int _patternAnimations;

        public string _rootFolderPrefabs = "Prefabs";
        public string _rootFolderScripts = "Scripts";
        public string _rootFolderTextures = "Textures";
        public string _rootFolderScenes = "Scenes";
        public string _rootFolderGraphics3D = "Graphics 3D";
        public string _rootFolderSounds = "Sounds";
        public string _rootFolderMaterials = "Materials";
        public string _rootFolderAnimations = "Animations";

        public List<string> _specialFolders = new List<string>();

        public List<string> _ignoreFolders = new List<string>();

        public List<string> _conditionFormula = new List<string>();
        public List<int> _conditionSelection = new List<int>();
    }
}