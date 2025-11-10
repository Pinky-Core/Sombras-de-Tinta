using System;
using UnityEngine;

/// <summary>
/// Data container for each tutorial popup entry.
/// </summary>
[Serializable]
public class TutorialPopupStep
{
    [SerializeField] private string title = "Tutorial";
    [SerializeField, TextArea(2, 4)] private string description = "Descripción";
    [SerializeField, Tooltip("Si es true, se pausará el juego mientras el popup está visible.")]
    private bool pauseGame = true;

    public string Title => title;
    public string Description => description;
    public bool PauseGame => pauseGame;
}
