using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GMTK.PlatformerToolkit {
    public class movementLimiter : MonoBehaviour {
        public static movementLimiter instance;

        [SerializeField] bool _initialCharacterCanMove = true;
        public bool characterCanMove;

        private Coroutine disableInputCoroutine;

        private void OnEnable() {
            instance = this;
        }

        private void Start() {
            characterCanMove = _initialCharacterCanMove;
        }

        public void DisableInputForDuration(float duration)
        {
            if (disableInputCoroutine != null)
            {
                StopCoroutine(disableInputCoroutine);
            }
            disableInputCoroutine = StartCoroutine(DisableInputCoroutine(duration));
        }

        private IEnumerator DisableInputCoroutine(float duration)
        {
            characterCanMove = false;
            yield return new WaitForSeconds(duration);
            characterCanMove = true;
            disableInputCoroutine = null;
        }
    }
}