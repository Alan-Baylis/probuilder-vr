using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine.InputNew;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Tools;
using UnityEditor.Experimental.EditorVR.Utilities;
using System.Reflection;

namespace ProBuilder2.VR
{
	/**
	 *	Defines some common behaviors among ProBuilderVR tools.
	 */
	public class ProBuilderToolBase : 	MonoBehaviour,
										IUsesRayOrigin,
										IConnectInterfaces,
										IInstantiateUI,
										ISelectTool,
										IUsesMenuOrigins
	{
		[SerializeField] private ProBuilderToolMenu m_ToolMenuPrefab;

		private GameObject m_ToolMenu;
		public Transform menuOrigin { get; set; }
		public Transform alternateMenuOrigin { get; set; }
		public Transform rayOrigin { get; set; }
		private EditorWindow m_VRView = null;
		private bool m_SendMouseEvent = true;
		
		// Disable the selection tool to turn of the hover highlight.  Not using
		// IExclusiveMode because we still want locomotion.
		private SelectionTool m_SelectionTool = null;

		private void Start()
		{
			if(m_ToolMenuPrefab == null)
			{
				Debug.LogError("Assign ProBuilder Tool Menu prefab on inheriting script!");
			}
			else
			{
				m_ToolMenu = this.InstantiateUI(m_ToolMenuPrefab.gameObject, alternateMenuOrigin, false);
				// m_ToolMenu = instantiateUI(m_ToolMenuPrefab.gameObject, alternateMenuOrigin, false);
				var toolsMenu = m_ToolMenu.GetComponent<ProBuilderToolMenu>();
				this.ConnectInterfaces(toolsMenu, rayOrigin);
				toolsMenu.onSelectTranslateTool += () => { this.SelectTool(rayOrigin, typeof(TranslateElementTool)); };
				toolsMenu.onSelectShapeTool += () => { this.SelectTool(rayOrigin, typeof(CreateShapeTool)); };
			}
				
			m_SelectionTool = gameObject.GetComponent<SelectionTool>();

			if(m_SelectionTool != null)
			{
				m_SelectionTool.enabled = false;
			}

			// In order to use some HandleUtility functions we'll need access to the OnSceneGUI delegate.  
			// This hooks up that event.
			// @todo - Ask Unity team about making this less hack-y.
			foreach(EditorWindow win in Resources.FindObjectsOfTypeAll<EditorWindow>())
			{
				if(win.GetType().ToString().Contains("VRView"))
				{
					m_VRView = win;
					Type vrViewType = win.GetType();
					EventInfo eventInfo = vrViewType.GetEvent("beforeOnGUI", BindingFlags.Public | BindingFlags.Static);
					Delegate onGUIDelegate = Delegate.CreateDelegate(eventInfo.EventHandlerType,
						this,
						typeof(ProBuilderToolBase).GetMethod("OnGUIInternal", BindingFlags.Instance | BindingFlags.NonPublic) );
					eventInfo.AddEventHandler(this, onGUIDelegate);
				}
			}

			pb_Start();
		}

		private void Update()
		{
#if UNITY_EDITOR
			// HACK - ripped from EditorVR pixelraycastmodule
			if (m_SendMouseEvent && m_VRView != null)
			{
				EditorApplication.delayCall += () =>
				{
					if (this != null) // Because this is a delay call, the component will be null when EditorVR closes
					{
						m_VRView.SendEvent(new Event() { type = EventType.MouseMove } );
					}
				};

				m_SendMouseEvent = false; // Don't allow another one to queue until the current one is processed
			}
#endif
		}

		private void OnGUIInternal(EditorWindow window)
		{
			pb_OnSceneGUI(window);
			m_SendMouseEvent = true;
		}

		private void OnDestroy()
		{
			foreach(EditorWindow win in Resources.FindObjectsOfTypeAll<EditorWindow>())
			{
				if(win.GetType().ToString().Contains("VRView"))
				{
					Type vrViewType = win.GetType();

					EventInfo eventInfo = vrViewType.GetEvent("beforeOnGUI", BindingFlags.Public | BindingFlags.Static);
					Delegate onGUIDelegate = Delegate.CreateDelegate(eventInfo.EventHandlerType,
						this,
						typeof(ProBuilderToolBase).GetMethod("OnGUIInternal", BindingFlags.Instance | BindingFlags.NonPublic) );
					eventInfo.RemoveEventHandler(this, onGUIDelegate);
				}
			}


			if(m_SelectionTool != null)
				m_SelectionTool.enabled = true;

			if(m_ToolMenu != null)
				ObjectUtils.Destroy(m_ToolMenu);

			pb_OnDestroy();
		}

		public virtual void pb_Start() {}

		public virtual void pb_OnSceneGUI(EditorWindow window) {}

		public virtual void pb_OnDestroy() {}
	}
}
