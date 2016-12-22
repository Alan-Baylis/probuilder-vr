﻿using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.VR.Utilities;

public class ObjectPlacementModule : MonoBehaviour
{
	private const float kInstantiateFOVDifference = 20f;

	private const float kGrowDuration = 0.5f;

	public void PositionPreview(Transform preview, Transform previewOrigin, float t = 1f)
	{
		preview.transform.position = Vector3.Lerp(preview.transform.position, previewOrigin.position, t);
		preview.transform.rotation = Quaternion.Lerp(preview.transform.rotation, previewOrigin.rotation, t);
	}

	public void PlaceObject(Transform obj, Vector3 targetScale)
	{
		StartCoroutine(PlaceObjectCoroutine(obj, targetScale));
	}

	private IEnumerator PlaceObjectCoroutine(Transform obj, Vector3 targetScale)
	{
		float start = Time.realtimeSinceStartup;
		var currTime = 0f;

		obj.parent = null;
		var startScale = obj.localScale;
		var startPosition = obj.position;

		//Get bounds at target scale
		var origScale = obj.localScale;
		obj.localScale = targetScale;
		var totalBounds = U.Object.GetTotalBounds(obj);
		obj.localScale = origScale;

		if (totalBounds != null)
		{
			// We want to position the object so that it fits within the camera perspective at its original scale
			var camera = U.Camera.GetMainCamera();
			var halfAngle = camera.fieldOfView * 0.5f;
			var perspective = halfAngle + kInstantiateFOVDifference;
			var camPosition = camera.transform.position;
			var forward = obj.position - camPosition;
			forward.y = 0;

			var distance = totalBounds.Value.size.magnitude / Mathf.Tan(perspective * Mathf.Deg2Rad);
			var destinationPosition = obj.position;
			if (distance > forward.magnitude)
				destinationPosition = camPosition + forward.normalized * distance;

			while (currTime < kGrowDuration)
			{
				currTime = Time.realtimeSinceStartup - start;
				var t = currTime / kGrowDuration;
				var tSquared = t * t;
				obj.localScale = Vector3.Lerp(startScale, targetScale, tSquared);
				obj.position = Vector3.Lerp(startPosition, destinationPosition, tSquared);
				yield return null;
			}
			obj.localScale = targetScale;
		}
		Selection.activeGameObject = obj.gameObject;
	}
}