using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using AOFL.Promises.V1.Core;
using AOFL.Promises.V1.Interfaces;
using System.Collections;
using System.Collections.Generic;

namespace Sojourn.Utility {
	public static class Utilities {
		public static IPromise PromiseGroup(params IPromise[] promises) {
			return new Promise().All(promises);
		}
		//same as above, but check for nulls
		public static IPromise PromiseGroupSafe(params IPromise[] promises) {
			List<IPromise> list = new List<IPromise>(promises.Length);
			foreach (IPromise p in promises) {
				if (p != null) { list.Add(p); }
			}
			return new Promise().All(list);
		}
	}
}