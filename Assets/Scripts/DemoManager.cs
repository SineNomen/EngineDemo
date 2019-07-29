using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class DemoManager : MonoBehaviour {
	[SerializeField]
	private float _engineSpeedMax = 30.0f;
	[SerializeField]
	private Animator _engineAnimator = null;
	[SerializeField]
	private Slider _engineSpeedSlider = null;
	[SerializeField]
	private Text _engineSpeedText = null;

	private float _engineSpeed = 7.0f;


	private void Start() {
		_engineSpeedSlider.minValue = 1.0f;
		_engineSpeedSlider.maxValue = _engineSpeedMax;
		_engineSpeedSlider.value = _engineSpeed;
		OnEngineSpeedChanged(_engineSpeedSlider.value);

		_engineSpeedSlider.onValueChanged.AddListener(OnEngineSpeedChanged);
	}

	private void OnEngineSpeedChanged(float val) {
		_engineSpeed = val;
		_engineAnimator.speed = _engineSpeed;
		_engineSpeedText.text = string.Format("Engine Speed: {0}", Mathf.RoundToInt(val));
	}
}
