﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;
using TMPro;

namespace EngineDemo {
	public class DemoManager : MonoBehaviour {
		[SerializeField]
		private CanvasGroup _splashGroup = null;
		[SerializeField]
		private float _engineSpeedMax = 30.0f;
		[SerializeField]
		private Animator _engineAnimator = null;
		[SerializeField]
		private Slider _engineSpeedSlider = null;
		[SerializeField]
		private TMP_Text _engineSpeedText = null;
		[SerializeField]
		private Orbitter _orbitter = null;
		[SerializeField]
		private Material _casingMaterial = null;
		[SerializeField]
		private ParticleSystem _exhaustEffect = null;
		[SerializeField]
		private ParticleSystem _intakeEffect = null;
		[SerializeField]
		private SimplePopup _torquePopup = null;
		[SerializeField]
		private SimplePopup _rpmPopup = null;
		[SerializeField]
		private SimplePopup _temperaturePopup = null;

		private float _engineSpeed = 7.0f;
		private float _lastTorque = 100.0f;
		private float _lastTemperature = 0.0f;

		private bool _showInteralOnZoom = true;

		private void Start() {
			_engineSpeedSlider.minValue = 1.0f;
			_engineSpeedSlider.maxValue = _engineSpeedMax;
			_engineSpeedSlider.value = _engineSpeed;
			OnEngineSpeedChanged(_engineSpeedSlider.value);

			_engineSpeedSlider.onValueChanged.AddListener(OnEngineSpeedChanged);

			_orbitter.OnZoom += OnZoom;
			StartCoroutine(UpdateTorque());
			StartCoroutine(UpdateRPM());
			StartCoroutine(UpdateOilTemp());
		}

		public void OnStartGame() {
			Sequence seq = DOTween.Sequence();
			seq.Append(_splashGroup.DOFade(0.0f, 0.25f));
			seq.Insert(0.2f, _orbitter.IntroTween());
			seq.AppendCallback(() => {
				_splashGroup.interactable = false;
				_splashGroup.blocksRaycasts = false;
			});

		}

		private void OnZoom(Orbitter orbitter) {
			Color color = _casingMaterial.color;
			if (_showInteralOnZoom) {
				color.a = orbitter.DistanceScale;
			} else {
				color.a = 1.0f;
			}
			_casingMaterial.color = color;
		}

		public void OnRepoButton() {
			Application.OpenURL("https://github.com/SineNomen/EngineDemo");
		}
		public void OnToggleShowInternal(bool val) {
			_showInteralOnZoom = val;
			OnZoom(_orbitter);
		}

		public void OnToggleShowLabels(bool val) {
			if (val) {
				_torquePopup.Show();
				_rpmPopup.Show();
				_temperaturePopup.Show();
			} else {
				_torquePopup.Hide();
				_rpmPopup.Hide();
				_temperaturePopup.Hide();
			}
		}

		private IEnumerator UpdateTorque() {
			while (true) {
				float adjust = Random.Range(-10, 10);
				float median = _engineSpeed * 10;
				float val = median + adjust;
				string color = "#FFFFFFFF";
				float delta = _lastTorque - median;
				if (adjust >= 4) {
					color = "#00FF33FF";
				} else if (adjust > -4) {
					color = "#FFFF33FF";
				} else {
					color = "#FF3333FF";
				}
				_torquePopup.Text = string.Format("Torque: <color={0}>{1}</color>Nm", color, Mathf.RoundToInt(val));
				yield return new WaitForSeconds(1.0f);
			}
		}

		private IEnumerator UpdateRPM() {
			yield return new WaitForSeconds(0.25f);
			while (true) {
				float adjust = Random.Range(-5, 5);
				float median = _engineSpeed;
				float val = (median + adjust);
				string color = "#FFFFFFFF";
				float delta = _lastTorque - median;
				if (adjust >= 4) {
					color = "#00FF33FF";
				} else if (adjust > -4) {
					color = "#FFFF33FF";
				} else {
					color = "#FF3333FF";
				}
				_rpmPopup.Text = string.Format("RPMs: <color={0}>{1}</color>K", color, Mathf.RoundToInt(val));
				yield return new WaitForSeconds(1.0f);
			}
		}

		private IEnumerator UpdateOilTemp() {
			yield return new WaitForSeconds(0.56f);
			while (true) {
				float adjust = Random.Range(-25.0f, 25.0f);
				float temp = 100.0f + adjust;
				string color = "#FFFFFFFF";
				if (adjust >= 15) {
					color = "#FF3333FF";
				} else if (adjust > -15) {
					color = "#FFFF33FF";
				} else {
					color = "#00FF33FF";
				}
				_temperaturePopup.Text = string.Format("Oil Temp: <color={0}>{1:0.0}</color> F", color, temp);
				yield return new WaitForSeconds(1.0f);
			}
		}

		private void OnEngineSpeedChanged(float val) {
			_engineSpeed = val;
			_engineAnimator.speed = _engineSpeed;
			_engineSpeedText.text = string.Format("Engine Speed: {0}", Mathf.RoundToInt(val));
			//`Mat magic number just to establish some kind of range
			_lastTorque = _engineSpeed * 10;
			//`Mat magic number, just add a little to make it have a trail
			ParticleSystem.MainModule exhaust = _exhaustEffect.main;
			exhaust.startSpeed = val + 4.0f;

			//`MAt magic numbers. Modify this particle based on the val, using the base params below
			ParticleSystem.MainModule intake = _intakeEffect.main;
			float baseSpeed = -10.0f;
			float baseLifetime = 1.0f;
			int baseRate = 20;
			float scaledVal = val * 0.5f;
			intake.startSpeed = scaledVal * baseSpeed;
			intake.startLifetime = baseLifetime / scaledVal;
			ParticleSystem.EmissionModule emission = _intakeEffect.emission;
			emission.rateOverTime = baseRate * scaledVal;
			_intakeEffect.Clear();
		}
	}
}