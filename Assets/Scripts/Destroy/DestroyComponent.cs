using System;
using System.Collections.Generic;
using UnityEngine;

public class DestroyComponent : MonoBehaviour
{
    public event Action OnDestructionStarted;
    public event Action OnDestructionComplete;

    [SerializeField] private GridComponent _grid;
    [SerializeField] private ElementFactory _elementFactory;
    [SerializeField] private DestroyAnimationComponent _animation;

    public void DestroyMatches(List<MatchData> matches)
    {
        if (matches == null || matches.Count == 0)
        {
            OnDestructionComplete?.Invoke();
            return;
        }

        OnDestructionStarted?.Invoke();

        var elementsToDestroy = CollectUniqueElements(matches);
        ClearCells(matches);

        _animation.AnimateDestroyGroup(elementsToDestroy, () =>
        {
            DestroyElements(elementsToDestroy);
            OnDestructionComplete?.Invoke();
        });
    }

    private List<ElementComponent> CollectUniqueElements(List<MatchData> matches)
    {
        var elements = new List<ElementComponent>();
        var processed = new HashSet<Cell>();

        foreach (var match in matches)
        {
            foreach (var cell in match.Cells)
            {
                if (processed.Contains(cell)) continue;
                if (cell.Element != null)
                {
                    elements.Add(cell.Element);
                    processed.Add(cell);
                }
            }
        }

        return elements;
    }

    private void ClearCells(List<MatchData> matches)
    {
        foreach (var match in matches)
        {
            foreach (var cell in match.Cells)
            {
                cell.Clear();
            }
        }
    }

    private void DestroyElements(List<ElementComponent> elements)
    {
        foreach (var element in elements)
        {
            _elementFactory.Destroy(element);
        }
    }
}
