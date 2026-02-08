using System;

public interface IGameOverClient
{
    /// <summary>
    /// Para limitar la oferta a 1 vez por partida (por minijuego).
    /// El minijuego lo resetea a false al empezar una run.
    /// </summary>
    bool HasUsedReviveOffer { get; set; }

    /// <summary>
    /// (Opcional) Congelar gameplay al fallar mientras sale la oferta.
    /// Si tu minijuego no necesita esto, deja vacío.
    /// </summary>
    void PauseOnFail();

    /// <summary>
    /// GameOver final propio del minijuego (UI, guardados, etc.)
    /// </summary>
    void FinalGameOver();

    /// <summary>
    /// Revive propio del minijuego (reset parcial, reanudar timers, etc.)
    /// </summary>
    void Revive();
}
