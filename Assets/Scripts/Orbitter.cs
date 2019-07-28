using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

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
	private Vector3 _lastMousePosition = Vector3.zero;
	private Rigidbody _body = null;
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
	}

	private void MoveByDelta(Vector3 delta) {
		delta.Scale(_speedScale);

		//rotating
		Vector3 newForward = Vector3.RotateTowards(transform.forward, transform.up, delta.y, 0.0f);
		_parent.angularVelocity += Vector3.up * -delta.x;
		_parent.angularVelocity += transform.right * delta.y;

		//zooming
		_distance = Mathf.Clamp(_distance + delta.z, _distanceLimit.x, _distanceLimit.y);
		transform.localPosition = Vector3.Lerp(transform.localPosition, new Vector3(0.0f, 0.0f, _distance), Time.deltaTime * 5);
	}

	private void Update() {
		transform.LookAt(_target);

		if (Input.GetMouseButtonDown(0)) { _lastMousePosition = Input.mousePosition; }

		Vector3 mouseDelta = Vector3.zero;
		if (Input.GetMouseButton(0)) {
			mouseDelta = (_lastMousePosition - Input.mousePosition);
		}

		mouseDelta.z = -Input.mouseScrollDelta.y;
		_lastMousePosition = Input.mousePosition;
		MoveByDelta(mouseDelta);
	}
}
