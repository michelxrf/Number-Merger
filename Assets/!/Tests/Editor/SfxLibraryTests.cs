using NUnit.Framework;
using NumberMerge.Audio;
using UnityEngine;

namespace NumberMerge.Audio.Tests
{
    public sealed class SfxLibraryTests
    {
        private SfxLibrary _library;

        [SetUp]
        public void SetUp()
        {
            _library = ScriptableObject.CreateInstance<SfxLibrary>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_library);
        }

        [Test]
        public void EmptyLibrary_ReturnsNullForEveryMoment()
        {
            foreach (SfxId id in System.Enum.GetValues(typeof(SfxId)))
            {
                Assert.IsNull(_library.GetRandomClip(id), id.ToString());
            }
        }

        [Test]
        public void AssignedClip_IsReturnedForItsMomentOnly()
        {
            AudioClip clip = AudioClip.Create("TestClip", 100, 1, 44100, false);
            SetClips(SfxId.TileInsert, clip);

            Assert.AreSame(clip, _library.GetRandomClip(SfxId.TileInsert));
            Assert.IsNull(_library.GetRandomClip(SfxId.GameOver));

            Object.DestroyImmediate(clip);
        }

        [Test]
        public void NullEntries_AreSkipped()
        {
            AudioClip clip = AudioClip.Create("TestClip", 100, 1, 44100, false);
            SetClips(SfxId.Combination, null, clip, null);

            for (int i = 0; i < 20; i++)
            {
                Assert.AreSame(clip, _library.GetRandomClip(SfxId.Combination));
            }

            Object.DestroyImmediate(clip);
        }

        private void SetClips(SfxId id, params AudioClip[] clips)
        {
            string field = id switch
            {
                SfxId.ButtonPress => "_buttonPress",
                SfxId.TileInsert => "_tileInsert",
                SfxId.Combination => "_combination",
                SfxId.GameStart => "_gameStart",
                SfxId.GameOver => "_gameOver",
                _ => null
            };

            var serialized = new UnityEditor.SerializedObject(_library);
            UnityEditor.SerializedProperty property = serialized.FindProperty(field);
            property.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
