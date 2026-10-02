namespace KJD.Game.PlayerController
{
    /// <summary>
    /// Interface pour les objets équipables qui remplacent le comportement standard (lancer/poser)
    /// par des actions dédiées au clic gauche et clic droit (ex: tirer avec l'arbalète, ré-enrouler).
    /// </summary>
    public interface ICustomActionHoldable
    {
        bool OverridesPrimaryAction { get; }
        bool OverridesSecondaryAction { get; }

        void OnPrimaryActionStarted();
        void OnPrimaryActionHeld();
        void OnPrimaryActionReleased();

        void OnSecondaryActionStarted();
        void OnSecondaryActionHeld();
        void OnSecondaryActionReleased();

        string GetPrimaryActionPrompt();
        string GetSecondaryActionPrompt();
    }
}
