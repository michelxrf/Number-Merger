using NUnit.Framework;
using NumberMerge.Gameplay;

namespace NumberMerge.Gameplay.Tests
{
    public sealed class MergeCeilingTests
    {
        [Test]
        public void TileAtCeiling_CannotBeCombined()
        {
            BoardModel board = TestBoards.Create(new[] { 16 }, new[] { 16 });
            var resolver = new MergeResolver(new SystemRandomSource(), 16);

            Assert.IsFalse(resolver.TryResolveOnce(board, out _));
            Assert.AreEqual(16, board.GetValue(0, 0));
            Assert.AreEqual(16, board.GetValue(1, 0));
        }

        [Test]
        public void MergeResultAboveCeiling_IsRejected()
        {
            BoardModel board = TestBoards.Create(new[] { 8 }, new[] { 8 });
            var resolver = new MergeResolver(new SystemRandomSource(), 8);

            Assert.IsFalse(resolver.TryResolveOnce(board, out _));
        }

        [Test]
        public void MergeReachingCeiling_IsAllowed()
        {
            BoardModel board = TestBoards.Create(new[] { 8 }, new[] { 8 });
            var resolver = new MergeResolver(new SystemRandomSource(), 16);

            Assert.IsTrue(resolver.TryResolveOnce(board, out _));
            Assert.AreEqual(16, board.GetValue(0, 0));
        }

        [Test]
        public void CeilingFromSettings_IsUsedByMerge()
        {
            var settings = new PieceGenerationSettings { MergeCeilingValue = 16 };
            BoardModel board = TestBoards.Create(new[] { 16 }, new[] { 16 });
            var resolver = new MergeResolver(new SystemRandomSource(), settings.MergeCeilingValue);

            Assert.IsFalse(resolver.TryResolveOnce(board, out _));
        }

        [Test]
        public void NonPowerOfTwoCeiling_IsRejected()
        {
            var settings = new PieceGenerationSettings { MergeCeilingValue = 100 };

            Assert.Throws<System.InvalidOperationException>(() => settings.Validate());
        }

        [Test]
        public void ZeroCeiling_IsRejected()
        {
            var settings = new PieceGenerationSettings { MergeCeilingValue = 0 };

            Assert.Throws<System.InvalidOperationException>(() => settings.Validate());
        }
    }
}
