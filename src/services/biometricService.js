// J.A.R.V.I.S. SYNAPSE — Trava biométrica (Windows Hello).
// Usa o bridge do Electron quando disponível; fora do desktop, cai para
// confirmação explícita do operador (nunca destrava sozinho).

class BiometricService {
  async isAvailable() {
    try {
      if (window.electronAPI?.helloAvailable) return !!(await window.electronAPI.helloAvailable());
    } catch { /* ignora */ }
    return false;
  }

  /** Retorna { ok, method } — method: 'windows-hello' | 'operator-confirm' | 'unavailable' */
  async unlock(reason = 'automação sensível') {
    // 1) Windows Hello via bridge nativo
    try {
      if (window.electronAPI?.helloVerify) {
        const res = await window.electronAPI.helloVerify(reason);
        if (res?.ok) return { ok: true, method: 'windows-hello' };
        return { ok: false, method: 'windows-hello' };
      }
    } catch { /* cai para confirmação */ }

    // 2) Fora do desktop / sem bridge: confirmação explícita do operador
    const confirmed = window.confirm(`J.A.R.V.I.S. — confirmação necessária\n\n${reason}\n\nConfirmar desbloqueio?`);
    return { ok: !!confirmed, method: 'operator-confirm' };
  }
}

export const biometricService = new BiometricService();

/** Ações que exigem biometria antes de executar. */
export const SENSITIVE_ACTIONS = new Set(['SYSTEM_POWER', 'PROCESS_KILL', 'CLOSE_RESOURCE']);
