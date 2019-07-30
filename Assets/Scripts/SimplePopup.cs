using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;
using TMPro;
using System;

namespace EngineDemo {
	[RequireComponent(typeof(CanvasGroup))]
	public class SimplePopup : MonoBehaviour {
		[SerializeField]
		private TMP_Text _text = null;
		[SerializeField]
		private bool _facePlayer = false;
		[SerializeField]
		private Camera _playerCamera = null;
		[SerializeField]
		private Transform _lineTarget = null;
		private CanvasGroup _canvasGroup;
		private float _fadeTime = 0.25f;

		public string Text { get => _text.text; set => _text.text = value; }

		private void Awake() {
			_canvasGroup = GetComponent<CanvasGroup>();
			if (_playerCamera == null && _facePlayer) {
				_playerCamera = Camera.main;
			}
		}

		public void Show() { _canvasGroup.DOFade(1.0f, _fadeTime); }
		public void Hide() { _canvasGroup.DOFade(0.0f, _fadeTime); }

		private void Update() {
			if (_facePlayer) {
				this.transform.forward = _playerCamera.transform.forward;
			}
		}
	}
}