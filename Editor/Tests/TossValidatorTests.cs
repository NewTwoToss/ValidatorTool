// =================================================================================================
//     Author:			Tomas "Toss" Szilagyi
//     Date created:	23.04.2018
// =================================================================================================

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TossValidatorTests
    {
        #region === 1 - ExampleOfNaming ===

        [Test]
        [TestCase("UnityProjectAssetName")]
        [TestCase("UnityP")]
        [TestCase("Unity")]
        [TestCase("Unity123")]
        [TestCase("UnityP")]
        [TestCase("Unity")]
        public void PatternOfNaming_Type01_CorrectExamples(string inputAssetName)
        {
            var regularExpression = "^[A-Z][a-z0-9]{0,}([A-Z][a-z0-9]{0,})*$";
            Assert.That(inputAssetName, Does.Match(regularExpression));
        }

        [Test]
        [TestCase("UnityProjectAssetName_")]
        [TestCase("UnityProjectAsset-Name")]
        [TestCase("xnity")]
        [TestCase("9Unity")]
        [TestCase("unity123")]
        [TestCase("123456")]
        [TestCase("9Unity")]
        public void PatternOfNaming_Type01_IncorrectExamples(string inputAssetName)
        {
            var regularExpression = "^[A-Z][a-z0-9]{0,}([A-Z][a-z0-9]{0,})*$";
            Assert.That(inputAssetName, Does.Not.Match(regularExpression));
        }

        #endregion

        #region === 2 - exampleOfNaming ===

        [Test]
        [TestCase("unityProjectAssetName")]
        [TestCase("unityP")]
        [TestCase("unity")]
        [TestCase("unityPro123")]
        [TestCase("unity123")]
        public void PatternOfNaming_Type02_CorrectExamples(string inputAssetName)
        {
            var regularExpression = "^[a-z][a-z0-9]{0,}([A-Z][a-z0-9]{0,})*$";
            Assert.That(inputAssetName, Does.Match(regularExpression));
        }

        [Test]
        [TestCase("UnityProjectAssetName")]
        [TestCase("UnityP")]
        [TestCase("Unity")]
        [TestCase("5xnity")]
        [TestCase("9Unity")]
        [TestCase("123unityTralala")]
        public void PatternOfNaming_Type02_IncorrectExamples(string inputAssetName)
        {
            var regularExpression = "^[a-z][a-z0-9]{0,}([A-Z][a-z0-9]{0,})*$";
            Assert.That(inputAssetName, Does.Not.Match(regularExpression));
        }

        #endregion

        #region === 3 - example_of_naming ===

        [Test]
        [TestCase("unity_project_asset_name")]
        [TestCase("unity_x456")]
        [TestCase("unity")]
        [TestCase("unity_4599")]
        public void PatternOfNaming_Type03_CorrectExamples(string inputAssetName)
        {
            var regularExpression = "^[a-z][a-z0-9]{0,}([_]{0,1}[a-z0-9]{1,})*$";
            Assert.That(inputAssetName, Does.Match(regularExpression));
        }

        [Test]
        [TestCase("unity_Project_")]
        [TestCase("_unity")]
        [TestCase("123_unity")]
        [TestCase("unity_")]
        [TestCase("unity_")]
        [TestCase("unity_aa_")]
        [TestCase("unity-a")]
        [TestCase("unity_Project_123")]
        public void PatternOfNaming_Type03_IncorrectExamples(string inputAssetName)
        {
            var regularExpression = "^[a-z][a-z0-9]{0,}([_]{0,1}[a-z0-9]{1,})*$";
            Assert.That(inputAssetName, Does.Not.Match(regularExpression));
        }

        #endregion

        #region === 4 - example-of-naming ===

        [Test]
        [TestCase("unity-project-asset-name")]
        [TestCase("unity")]
        [TestCase("unity123")]
        [TestCase("unity-a")]
        [TestCase("unity-007")]
        public void PatternOfNaming_Type04_CorrectExamples(string inputAssetName)
        {
            var regularExpression = "^[a-z][a-z0-9]{0,}([-]{0,1}[a-z0-9]{1,})*$";
            Assert.That(inputAssetName, Does.Match(regularExpression));
        }

        [Test]
        [TestCase("unity-Project-")]
        [TestCase("-unity")]
        [TestCase("unity-")]
        [TestCase("unity-A")]
        [TestCase("unity-A1234")]
        [TestCase("unity-Project")]
        public void PatternOfNaming_Type04_IncorrectExamples(string inputAssetName)
        {
            var regularExpression = "^[a-z][a-z0-9]{0,}([-]{0,1}[a-z0-9]{1,})*$";
            Assert.That(inputAssetName, Does.Not.Match(regularExpression));
        }

        #endregion

        #region === 5 - Example_Of_Naming ===

        [Test]
        [TestCase("Unity_Project_Asset_Name")]
        [TestCase("Unity")]
        [TestCase("Xx_Unity_01")]
        [TestCase("Xx_Unity0008")]
        public void PatternOfNaming_Type05_CorrectExamples(string inputAssetName)
        {
            var regularExpression = "^[A-Z][a-z0-9]{0,}([_]{0,1}[A-Z0-9][a-z0-9]{1,})*$";
            Assert.That(inputAssetName, Does.Match(regularExpression));
        }

        [Test]
        [TestCase("unity_Project_Asset_Name")]
        [TestCase("unity_Project_")]
        [TestCase("_unity")]
        [TestCase("123_unity")]
        [TestCase("unity_")]
        [TestCase("unity_")]
        [TestCase("unity_aa_")]
        [TestCase("unity-a")]
        [TestCase("unity_Aaaa")]
        [TestCase("unity_project_asset_name")]
        [TestCase("unity")]
        [TestCase("XX_unity")]
        public void PatternOfNaming_Type05_IncorrectExamples(string inputAssetName)
        {
            var regularExpression = "^[A-Z][a-z0-9]{0,}([_]{0,1}[A-Z0-9][a-z0-9]{1,})*$";
            Assert.That(inputAssetName, Does.Not.Match(regularExpression));
        }

        #endregion

        //==========================================================================================
        [Test]
        [TestCase(".png")]
        [TestCase(".jpg")]
        [TestCase(".jpeg")]
        public void PatternOfNaming_PlusAssetSuffix(string suffix)
        {
            var inputAssetName = "my_project_prefab" + suffix;
            var regularExpression = "^[a-z][a-z0-9]{0,}([_]{0,1}[a-z0-9]{1,})*";
            var regularExpressionPlusSuffix = regularExpression + "(.png|.jpg|.jpeg)$";

            Assert.That(inputAssetName, Does.Match(regularExpressionPlusSuffix));
            Assert.That(inputAssetName, Does.Match(regularExpressionPlusSuffix));
        }
    }
}