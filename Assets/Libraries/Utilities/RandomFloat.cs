using Sojourn.PicnicIOC;
using Sojourn.Extensions;
using UnityEngine;
using AOFL.Promises.V1.Core;
using AOFL.Promises.V1.Interfaces;
using System.Collections;
using System.Collections.Generic;


//`Mat add option for a limit to the lowest min and highest max
namespace Sojourn.Utility {
	[System.Serializable]
	public struct RandomFloat {
		private float Value;
		[SerializeField]
		public float Min;
		[SerializeField]
		public float Max;
		[SerializeField]
		public float Low;
		[SerializeField]
		public float High;

		public RandomFloat(float min, float max, float low = -Mathf.Infinity, float high = Mathf.Infinity) {
			Min = min;
			Max = max;
			Low = low;
			High = high;
			if (Max < Min) {
				Debug.LogError("Max is less than min");
			}
			Value = -42.42424242f;
		}

		public float Pick() {
			Value = Random.Range(Min, Max);
			return Value;
		}

		public override string ToString() { return Value.ToString(); }
		public static implicit operator float(RandomFloat rf) { return rf.Value; }
	}
}