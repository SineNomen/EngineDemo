using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;
using DG.Tweening;

public delegate void OrbitEvent(Orbitter orbitter);
//orbits the target, always looking at it, can be made to move further or close
public class Orbitter : MonoBehaviour {
	[SerializeField]
	private Transform _parent = null;
	[SerializeField]
	private Transform _target = null;
	[SerializeField]
	private Vector3 _speedScale = Vector3.one;
	[SerializeField]
	private Vector2 _distanceLimit = new Vector2(1.0f, 100.0f);
	[SerializeField]
	private float _scrollTime = 0.25f;
	[SerializeField]
	private float _spinTime = 0.5f;

	private float _distance = 0.0f;
	private Vector3 _rotation = Vector3.zero;
	public OrbitEvent OnZoom { get; set; }
	public float DistanceScale {
		get {
			return (_distance - _distanceLimit.x) / (_distanceLimit.y - _distanceLimit.x);
		}
	}

	private void Start() {
		//start in the middle of our range
		_parent.position = _target.position;
		_distance = _distanceLimit.y;
		_rotation = transform.rotation.eulerAngles;
		transform.position = Vector3.forward * _distanceLimit.y;

		MoveByDelta(Vector3.zero);
		transform.LookAt(_target);
	}

	private void MoveByDelta(Vector2 delta) {
		delta.Scale(_speedScale);

		//rotating
		Vector3 vel = (Vector3.up * delta.x) + (Vector3.right * delta.y);
		_rotation += vel;
		DOTween.Kill(_parent);
		_parent.DORotate(_rotation, _spinTime);
		// _parent.rotation = Quaternion.Euler(_rotation);
	}

	private void GotoDistanceDelta(float delta) {
		//zooming
		_distance = Mathf.Clamp(_distance + (delta * _speedScale.z), _distanceLimit.x, _distanceLimit.y);
		transform.DOLocalMove(new Vector3(0.0f, 0.0f, _distance), _scrollTime);
		if (OnZoom != null) {
			OnZoom(this);
		}
	}

	public Tween IntroTween() {
		Sequence seq = DOTween.Sequence();
		return seq;
	}

	public void OnPointerDrag(BaseEventData baseData) {
		PointerEventData eventData = (PointerEventData)baseData;
		MoveByDelta(eventData.delta);
	}

	public void OnScroll(BaseEventData baseData) {
		PointerEventData eventData = (PointerEventData)baseData;
		GotoDistanceDelta(-eventData.scrollDelta.y);
	}
}
