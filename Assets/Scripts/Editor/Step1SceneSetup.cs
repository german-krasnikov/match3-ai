// Editor/Step1SceneSetup.cs
using UnityEngine;
using UnityEditor;
using Features.Board.Models;
using Common;

namespace Editor
{
    public static class Step1SceneSetup
    {
        [MenuItem("Setup/Step 1 - Core + Grid Infrastructure")]
        public static void Setup()
        {
            // Step 1 is pure C# - no scene objects needed
            // Just validate that BoardModel works

            var board = new BoardModel(8, 8);

            // Test basic operations
            var pos = new GridPosition(0, 0);
            board.SetElement(pos, new Element(ElementType.Red));
            var element = board.GetElement(pos);

            if (element != null && element.Type == ElementType.Red)
            {
                Debug.Log("[Step 1] BoardModel validation PASSED");
                Debug.Log($"  - Grid size: {board.Width}x{board.Height}");
                Debug.Log($"  - Element at (0,0): {element.Type}");
            }
            else
            {
                Debug.LogError("[Step 1] BoardModel validation FAILED");
            }
        }
    }
}
