﻿using System;
using System.Collections;
using ListView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;

public class AssetGridItem : ListViewItem<AssetData>, IPlaceObjects, IPositionPreview
{
	private const float kMagnetizeDuration = 0.5f;
	private const float kPreviewDuration = 0.1f;

	private const float kRotateSpeed = 50f;

	[SerializeField]
	private Text m_Text;

	[SerializeField]
	private BaseHandle m_Handle;

	[SerializeField]
	private Image m_TextPanel;

	[SerializeField]
	private Renderer m_Cube;

	[SerializeField]
	private Renderer m_Sphere;

	[HideInInspector]
	[SerializeField] // Serialized so that this remains set after cloning
	private GameObject m_Icon;

	private GameObject m_IconPrefab;

	[HideInInspector]
	[SerializeField] // Serialized so that this remains set after cloning
	private Transform m_PreviewObject;

	private bool m_Setup;
	private Transform m_GrabbedObject;
	private float m_GrabLerp;
	private float m_PreviewFade;
	private Vector3 m_PreviewPrefabScale;
	private Vector3 m_PreviewTargetScale;

	private Coroutine m_TransitionCoroutine;

	private Material m_TextureMaterial;

	public GameObject icon
	{
		private get
		{
			if (m_Icon)
				return m_Icon;
			return m_Cube.gameObject;
		}
		set
		{
			m_Cube.gameObject.SetActive(false);
			m_Sphere.gameObject.SetActive(false);

			if (m_IconPrefab == value)
			{
				m_Icon.SetActive(true);
				return;
			}

			if(m_Icon)
				U.Object.Destroy(m_Icon);

			m_IconPrefab = value;
			m_Icon = U.Object.Instantiate(m_IconPrefab, transform, false);
			m_Icon.transform.localPosition = Vector3.up * 0.5f;
			m_Icon.transform.localRotation = Quaternion.AngleAxis(90, Vector3.down);
			m_Icon.transform.localScale = Vector3.one;
		}
	}

	public Material material
	{
		set
		{
			m_Sphere.sharedMaterial = value;
			m_Sphere.gameObject.SetActive(true);
			m_Cube.gameObject.SetActive(false);
			if(m_Icon)
				m_Icon.gameObject.SetActive(false);
		}
	}

	public Texture texture
	{
		set
		{
			m_Sphere.gameObject.SetActive(true);
			m_Cube.gameObject.SetActive(false);
			if(m_Icon)
				m_Icon.gameObject.SetActive(false);
			if (!value)
			{
				m_Sphere.sharedMaterial.mainTexture = null;
				return;
			}
			if(m_TextureMaterial)
				U.Object.Destroy(m_TextureMaterial);

			m_TextureMaterial = new Material(Shader.Find("Standard")) { mainTexture = value };
			m_Sphere.sharedMaterial = m_TextureMaterial;
		}
	}

	public Texture fallbackTexture
	{
		set
		{
			if(value)
				value.wrapMode = TextureWrapMode.Clamp;

			m_Cube.sharedMaterial.mainTexture = value;
			m_Cube.gameObject.SetActive(true);
			m_Sphere.gameObject.SetActive(false);

			if (m_Icon)
				m_Icon.gameObject.SetActive(false);
		}
	}

	public Action<Transform, Vector3> placeObject { private get; set; }

	public Func<Transform, Transform> getPreviewOriginForRayOrigin { private get; set; }
	public PositionPreviewDelegate positionPreview { private get; set; }

	public override void Setup(AssetData listData)
	{
		base.Setup(listData);
		// First time setup
		if (!m_Setup)
		{
			// Cube material might change, so we always instance it
			U.Material.GetMaterialClone(m_Cube);

			m_Handle.dragStarted += OnGrabStarted;
			m_Handle.dragging += OnGrabDragging;
			m_Handle.dragEnded += OnGrabEnded;

			m_Handle.hoverStarted += OnHoverStarted;
			m_Handle.hoverEnded += OnHoverEnded;

			m_Setup = true;
		}

		InstantiatePreview();

		m_Text.text = listData.name;
		m_PreviewFade = 0;
	}

	public void UpdateTransforms(float scale)
	{
		transform.localScale = Vector3.one * scale;

		m_TextPanel.transform.localRotation = U.Camera.LocalRotateTowardCamera(transform.parent.rotation);

		// Handle preview fade
		if (m_PreviewObject)
		{
			if (m_PreviewFade == 0)
			{
				m_PreviewObject.gameObject.SetActive(false);
				icon.SetActive(true);
				icon.transform.localScale = Vector3.one;
			}
			else if (m_PreviewFade == 1)
			{
				m_PreviewObject.gameObject.SetActive(true);
				icon.SetActive(false);
				m_PreviewObject.transform.localScale = m_PreviewTargetScale;
			}
			else
			{
				icon.SetActive(true);
				m_PreviewObject.gameObject.SetActive(true);
				icon.transform.localScale = Vector3.one * (1 - m_PreviewFade);
				m_PreviewObject.transform.localScale = Vector3.Lerp(Vector3.zero, m_PreviewTargetScale, m_PreviewFade);
			}
		}

		if (m_Sphere.gameObject.activeInHierarchy)
			m_Sphere.transform.Rotate(Vector3.up, kRotateSpeed * Time.unscaledDeltaTime, Space.Self);

		if (data.type == "Scene")
		{
			icon.transform.rotation = Quaternion.LookRotation(icon.transform.position - U.Camera.GetMainCamera().transform.position, Vector3.up);
		}
	}

	private void InstantiatePreview()
	{
		if (m_PreviewObject)
			U.Object.Destroy(m_PreviewObject.gameObject);
		if (!data.preview)
			return;

		m_PreviewObject = Instantiate(data.preview).transform;
		m_PreviewObject.position = Vector3.zero;
		m_PreviewObject.rotation = Quaternion.identity;

		m_PreviewPrefabScale = m_PreviewObject.localScale;

		// Normalize total scale to 1
		var previewTotalBounds = U.Object.GetTotalBounds(m_PreviewObject);

		// Don't show a preview if there are no renderers
		if (previewTotalBounds == null)
		{
			U.Object.Destroy(m_PreviewObject.gameObject);
			return;
		}

		m_PreviewObject.SetParent(transform, false);

		m_PreviewTargetScale = m_PreviewPrefabScale * (1 / previewTotalBounds.Value.size.MaxComponent());
		m_PreviewObject.localPosition = Vector3.up * 0.5f;

		m_PreviewObject.gameObject.SetActive(false);
		m_PreviewObject.localScale = Vector3.zero;
	}

	private void OnGrabStarted(BaseHandle baseHandle, HandleEventData eventData)
	{
		var clone = (GameObject) Instantiate(gameObject, transform.position, transform.rotation, transform.parent);
		var cloneItem = clone.GetComponent<AssetGridItem>();

		if (cloneItem.m_PreviewObject)
		{
			cloneItem.m_Cube.gameObject.SetActive(false);
			if(cloneItem.m_Icon)
				cloneItem.m_Icon.gameObject.SetActive(false);
			cloneItem.m_PreviewObject.gameObject.SetActive(true);
			cloneItem.m_PreviewObject.transform.localScale = m_PreviewTargetScale;

			// Destroy label
			U.Object.Destroy(cloneItem.m_TextPanel.gameObject);
		}

		m_GrabbedObject = clone.transform;
		m_GrabLerp = 0;
		StartCoroutine(Magnetize());
	}

	// Smoothly interpolate grabbed object into position, instead of "popping."
	private IEnumerator Magnetize()
	{
		var startTime = Time.realtimeSinceStartup;
		var currTime = 0f;
		while (currTime < kMagnetizeDuration)
		{
			currTime = Time.realtimeSinceStartup - startTime;
			m_GrabLerp = currTime / kMagnetizeDuration;
			yield return null;
		}
		m_GrabLerp = 1;
	}

	private void OnGrabDragging(BaseHandle baseHandle, HandleEventData eventData)
	{
		positionPreview(m_GrabbedObject.transform, getPreviewOriginForRayOrigin(eventData.rayOrigin), m_GrabLerp);
	}

	private void OnGrabEnded(BaseHandle baseHandle, HandleEventData eventData)
	{
		var gridItem = m_GrabbedObject.GetComponent<AssetGridItem>();
		if (gridItem.m_PreviewObject)
			placeObject(gridItem.m_PreviewObject, m_PreviewPrefabScale);
		else
		{
			switch (data.type)
			{
				case "Prefab":
					Instantiate(data.asset, gridItem.transform.position, gridItem.transform.rotation);
					break;
				case "Model":
					Instantiate(data.asset, gridItem.transform.position, gridItem.transform.rotation);
					break;
			}
		}
		gridItem.m_Cube.sharedMaterial = null; // Drop material so it won't be destroyed (shared with cube in list)
		U.Object.Destroy(m_GrabbedObject.gameObject);
	}

	private void OnHoverStarted(BaseHandle baseHandle, HandleEventData eventData)
	{
		if (gameObject.activeInHierarchy)
		{
			if (m_TransitionCoroutine != null)
				StopCoroutine(m_TransitionCoroutine);
			m_TransitionCoroutine = StartCoroutine(AnimatePreview(false));
		}
	}

	private void OnHoverEnded(BaseHandle baseHandle, HandleEventData eventData)
	{
		if (gameObject.activeInHierarchy)
		{
			if (m_TransitionCoroutine != null)
				StopCoroutine(m_TransitionCoroutine);
			m_TransitionCoroutine = StartCoroutine(AnimatePreview(true));
		}
	}

	private IEnumerator AnimatePreview(bool @out)
	{
		var startVal = 0;
		var endVal = 1;
		if (@out)
		{
			startVal = 1;
			endVal = 0;
		}
		var startTime = Time.realtimeSinceStartup;
		while (Time.realtimeSinceStartup - startTime < kPreviewDuration)
		{
			m_PreviewFade = Mathf.Lerp(startVal, endVal, (Time.realtimeSinceStartup - startTime) / kPreviewDuration);
			yield return null;
		}
		m_PreviewFade = endVal;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_Cube.sharedMaterial);
	}
}