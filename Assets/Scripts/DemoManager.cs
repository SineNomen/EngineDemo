using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System;

public class DemoManager : MonoBehaviour {
	[SerializeField]
	private float _engineSpeedMax = 30.0f;
	[SerializeField]
	private Animator _engineAnimator = null;
	[SerializeField]
	private Slider _engineSpeedSlider = null;
	[SerializeField]
	private Text _engineSpeedText = null;
	[SerializeField]
	private Orbitter _orbitter = null;
	[SerializeField]
	private Material _casingMaterial = null;
	[SerializeField]
	private ParticleSystem _exhaustEffect = null;

	private float _engineSpeed = 7.0f;

	private bool _showInteralOnZoom = true;

	private void Start() {
		_engineSpeedSlider.minValue = 1.0f;
		_engineSpeedSlider.maxValue = _engineSpeedMax;
		_engineSpeedSlider.value = _engineSpeed;
		OnEngineSpeedChanged(_engineSpeedSlider.value);

		_engineSpeedSlider.onValueChanged.AddListener(OnEngineSpeedChanged);

		_orbitter.OnZoom += OnZoom;
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

	public void OnToggleShowInternal(bool val) {
		_showInteralOnZoom = val;
		OnZoom(_orbitter);
	}

	private void OnEngineSpeedChanged(float val) {
		_engineSpeed = val;
		_engineAnimator.speed = _engineSpeed;
		_engineSpeedText.text = string.Format("Engine Speed: {0}", Mathf.RoundToInt(val));
		ParticleSystem.MainModule main = _exhaustEffect.main;
		//`Mat hack, magic number, just add a little to make it have a trail
		main.startSpeed = val + 4.0f;
	}
}
