using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;
using DG.Tweening;

//orbits the target, always looking at it, can be made to move further or close
public class Orbitter : MonoBehaviour {
	[SerializeField]
	private Rigidbody _parent = null;
	[SerializeField]
	private Transform _target = null;
	[SerializeField]
	private Vector3 _speedScale = Vector3.one;
	[SerializeField]
	private Vector2 _distanceLimit = new Vector2(1.0f, 100.0f);

	private float _distance = 0.0f;
	private Rigidbody _body = null;
	private float _scrollTime = 0.25f;
	private Coroutine _faceHandle = null;
	public float DistanceScale {
		get {
			return (_distance - _distanceLimit.x) / (_distanceLimit.y - _distanceLimit.x);
		}
	}

	private void Start() {
		//start in the middle of our range
		_parent.transform.position = _target.position;
		transform.position = Vector3.forward * (_distanceLimit.x + ((_distanceLimit.y - _distanceLimit.x) * 0.5f));

		MoveByDelta(Vector3.zero);
		transform.LookAt(_target);
	}

	private void MoveByDelta(Vector2 delta) {
		delta.Scale(_speedScale);

		//rotating
		Vector3 newForward = Vector3.RotateTowards(transform.forward, transform.up, delta.y, 0.0f);
		_parent.angularVelocity += Vector3.up * delta.x;
		_parent.angularVelocity += transform.right * -delta.y;
	}

	//`MAt this is used to avoid a slow Update ()
	private IEnumerator FaceTarget() {
		while (!_parent.IsSleeping()) {
			transform.LookAt(_target, Vector3.up);
			yield return null;
		}
	}

	private void Update() {
		Debug.Log(transform.up);
	}

	private void GotoDistanceDelta(float delta) {
		//zooming
		_distance = Mathf.Clamp(_distance + (delta * _speedScale.z), _distanceLimit.x, _distanceLimit.y);
		transform.DOLocalMove(new Vector3(0.0f, 0.0f, _distance), _scrollTime);
	}

	public void OnPointerDown(BaseEventData baseData) {
		//only run FaceTarget while we are moving, once we stop (even if zooming), there's no need
		if (_faceHandle != null) { StopCoroutine(_faceHandle); }
		_faceHandle = StartCoroutine(FaceTarget());
	}

	public void OnPointerUp(BaseEventData baseData) {
		MoveByDelta(Vector2.zero);
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
