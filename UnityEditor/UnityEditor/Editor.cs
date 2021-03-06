using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEditor
{
	[RequiredByNativeCode]
	[StructLayout(LayoutKind.Sequential)]
	public class Editor : ScriptableObject, IPreviewable
	{
		private class Styles
		{
			public GUIStyle inspectorBig = new GUIStyle(EditorStyles.inspectorBig);

			public GUIStyle inspectorBigInner = new GUIStyle("IN BigTitle inner");

			public GUIStyle centerStyle = new GUIStyle();

			public Styles()
			{
				this.centerStyle.alignment = TextAnchor.MiddleCenter;
				this.inspectorBig.padding.bottom--;
			}
		}

		private UnityEngine.Object[] m_Targets;

		private UnityEngine.Object m_Context;

		private int m_IsDirty;

		private int m_ReferenceTargetIndex = 0;

		private PropertyHandlerCache m_PropertyHandlerCache = new PropertyHandlerCache();

		private IPreviewable m_DummyPreview;

		internal SerializedObject m_SerializedObject = null;

		private OptimizedGUIBlock m_OptimizedBlock;

		internal InspectorMode m_InspectorMode = InspectorMode.Normal;

		internal const float kLineHeight = 16f;

		internal bool hideInspector = false;

		internal static bool m_AllowMultiObjectAccess = true;

		private static Editor.Styles s_Styles;

		private const float kImageSectionWidth = 44f;

		internal bool canEditMultipleObjects
		{
			get
			{
				return base.GetType().GetCustomAttributes(typeof(CanEditMultipleObjects), false).Length > 0;
			}
		}

		public UnityEngine.Object target
		{
			get
			{
				return this.m_Targets[this.referenceTargetIndex];
			}
			set
			{
				throw new InvalidOperationException("You can't set the target on an editor.");
			}
		}

		public UnityEngine.Object[] targets
		{
			get
			{
				if (!Editor.m_AllowMultiObjectAccess)
				{
					Debug.LogError("The targets array should not be used inside OnSceneGUI or OnPreviewGUI. Use the single target property instead.");
				}
				return this.m_Targets;
			}
		}

		internal virtual int referenceTargetIndex
		{
			get
			{
				return Mathf.Clamp(this.m_ReferenceTargetIndex, 0, this.m_Targets.Length - 1);
			}
			set
			{
				this.m_ReferenceTargetIndex = (Math.Abs(value * this.m_Targets.Length) + value) % this.m_Targets.Length;
			}
		}

		internal virtual string targetTitle
		{
			get
			{
				string result;
				if (this.m_Targets.Length == 1 || !Editor.m_AllowMultiObjectAccess)
				{
					result = this.target.name;
				}
				else
				{
					result = string.Concat(new object[]
					{
						this.m_Targets.Length,
						" ",
						ObjectNames.NicifyVariableName(ObjectNames.GetClassName(this.target)),
						"s"
					});
				}
				return result;
			}
		}

		public SerializedObject serializedObject
		{
			get
			{
				if (!Editor.m_AllowMultiObjectAccess)
				{
					Debug.LogError("The serializedObject should not be used inside OnSceneGUI or OnPreviewGUI. Use the target property directly instead.");
				}
				return this.GetSerializedObjectInternal();
			}
		}

		internal bool isInspectorDirty
		{
			get
			{
				return this.m_IsDirty != 0;
			}
			set
			{
				this.m_IsDirty = ((!value) ? 0 : 1);
			}
		}

		internal virtual IPreviewable preview
		{
			get
			{
				if (this.m_DummyPreview == null)
				{
					this.m_DummyPreview = new ObjectPreview();
					this.m_DummyPreview.Initialize(this.targets);
				}
				return this.m_DummyPreview;
			}
		}

		internal PropertyHandlerCache propertyHandlerCache
		{
			get
			{
				return this.m_PropertyHandlerCache;
			}
		}

		[GeneratedByOldBindingsGenerator]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern Editor CreateEditorWithContext(UnityEngine.Object[] targetObjects, UnityEngine.Object context, [DefaultValue("null")] Type editorType);

		[ExcludeFromDocs]
		public static Editor CreateEditorWithContext(UnityEngine.Object[] targetObjects, UnityEngine.Object context)
		{
			Type editorType = null;
			return Editor.CreateEditorWithContext(targetObjects, context, editorType);
		}

		public static void CreateCachedEditorWithContext(UnityEngine.Object targetObject, UnityEngine.Object context, Type editorType, ref Editor previousEditor)
		{
			Editor.CreateCachedEditorWithContext(new UnityEngine.Object[]
			{
				targetObject
			}, context, editorType, ref previousEditor);
		}

		public static void CreateCachedEditorWithContext(UnityEngine.Object[] targetObjects, UnityEngine.Object context, Type editorType, ref Editor previousEditor)
		{
			if (!(previousEditor != null) || !ArrayUtility.ArrayEquals<UnityEngine.Object>(previousEditor.m_Targets, targetObjects) || !(previousEditor.m_Context == context))
			{
				if (previousEditor != null)
				{
					UnityEngine.Object.DestroyImmediate(previousEditor);
				}
				previousEditor = Editor.CreateEditorWithContext(targetObjects, context, editorType);
			}
		}

		public static void CreateCachedEditor(UnityEngine.Object targetObject, Type editorType, ref Editor previousEditor)
		{
			Editor.CreateCachedEditorWithContext(new UnityEngine.Object[]
			{
				targetObject
			}, null, editorType, ref previousEditor);
		}

		public static void CreateCachedEditor(UnityEngine.Object[] targetObjects, Type editorType, ref Editor previousEditor)
		{
			Editor.CreateCachedEditorWithContext(targetObjects, null, editorType, ref previousEditor);
		}

		[ExcludeFromDocs]
		public static Editor CreateEditor(UnityEngine.Object targetObject)
		{
			Type editorType = null;
			return Editor.CreateEditor(targetObject, editorType);
		}

		public static Editor CreateEditor(UnityEngine.Object targetObject, [DefaultValue("null")] Type editorType)
		{
			return Editor.CreateEditorWithContext(new UnityEngine.Object[]
			{
				targetObject
			}, null, editorType);
		}

		[ExcludeFromDocs]
		public static Editor CreateEditor(UnityEngine.Object[] targetObjects)
		{
			Type editorType = null;
			return Editor.CreateEditor(targetObjects, editorType);
		}

		public static Editor CreateEditor(UnityEngine.Object[] targetObjects, [DefaultValue("null")] Type editorType)
		{
			return Editor.CreateEditorWithContext(targetObjects, null, editorType);
		}

		internal virtual SerializedObject GetSerializedObjectInternal()
		{
			if (this.m_SerializedObject == null)
			{
				this.m_SerializedObject = new SerializedObject(this.targets, this.m_Context);
			}
			return this.m_SerializedObject;
		}

		private void CleanupPropertyEditor()
		{
			if (this.m_OptimizedBlock != null)
			{
				this.m_OptimizedBlock.Dispose();
				this.m_OptimizedBlock = null;
			}
			if (this.m_SerializedObject != null)
			{
				this.m_SerializedObject.Dispose();
				this.m_SerializedObject = null;
			}
		}

		private void OnDisableINTERNAL()
		{
			this.CleanupPropertyEditor();
		}

		internal virtual void OnForceReloadInspector()
		{
			if (this.m_SerializedObject != null)
			{
				this.m_SerializedObject.SetIsDifferentCacheDirty();
			}
		}

		internal bool GetOptimizedGUIBlockImplementation(bool isDirty, bool isVisible, out OptimizedGUIBlock block, out float height)
		{
			if (this.m_OptimizedBlock == null)
			{
				this.m_OptimizedBlock = new OptimizedGUIBlock();
			}
			block = this.m_OptimizedBlock;
			bool result;
			if (!isVisible)
			{
				height = 0f;
				result = true;
			}
			else
			{
				if (this.m_SerializedObject == null)
				{
					this.m_SerializedObject = new SerializedObject(this.targets, this.m_Context);
				}
				else
				{
					this.m_SerializedObject.Update();
				}
				this.m_SerializedObject.inspectorMode = this.m_InspectorMode;
				SerializedProperty iterator = this.m_SerializedObject.GetIterator();
				height = 2f;
				bool enterChildren = true;
				while (iterator.NextVisible(enterChildren))
				{
					height += EditorGUI.GetPropertyHeight(iterator, null, true) + 2f;
					enterChildren = false;
				}
				if (height == 2f)
				{
					height = 0f;
				}
				result = true;
			}
			return result;
		}

		internal bool OptimizedInspectorGUIImplementation(Rect contentRect)
		{
			SerializedProperty iterator = this.m_SerializedObject.GetIterator();
			bool enterChildren = true;
			bool enabled = GUI.enabled;
			contentRect.xMin += 14f;
			contentRect.xMax -= 4f;
			contentRect.y += 2f;
			while (iterator.NextVisible(enterChildren))
			{
				contentRect.height = EditorGUI.GetPropertyHeight(iterator, null, false);
				EditorGUI.indentLevel = iterator.depth;
				using (new EditorGUI.DisabledScope(this.m_InspectorMode == InspectorMode.Normal && "m_Script" == iterator.propertyPath))
				{
					enterChildren = EditorGUI.PropertyField(contentRect, iterator);
				}
				contentRect.y += contentRect.height + 2f;
			}
			GUI.enabled = enabled;
			return this.m_SerializedObject.ApplyModifiedProperties();
		}

		protected internal static void DrawPropertiesExcluding(SerializedObject obj, params string[] propertyToExclude)
		{
			SerializedProperty iterator = obj.GetIterator();
			bool enterChildren = true;
			while (iterator.NextVisible(enterChildren))
			{
				enterChildren = false;
				if (!propertyToExclude.Contains(iterator.name))
				{
					EditorGUILayout.PropertyField(iterator, true, new GUILayoutOption[0]);
				}
			}
		}

		public bool DrawDefaultInspector()
		{
			return this.DoDrawDefaultInspector();
		}

		public virtual void OnInspectorGUI()
		{
			this.DrawDefaultInspector();
		}

		public virtual bool RequiresConstantRepaint()
		{
			return false;
		}

		internal void InternalSetTargets(UnityEngine.Object[] t)
		{
			this.m_Targets = t;
		}

		internal void InternalSetHidden(bool hidden)
		{
			this.hideInspector = hidden;
		}

		internal void InternalSetContextObject(UnityEngine.Object context)
		{
			this.m_Context = context;
		}

		internal virtual bool GetOptimizedGUIBlock(bool isDirty, bool isVisible, out OptimizedGUIBlock block, out float height)
		{
			block = null;
			height = -1f;
			return false;
		}

		internal virtual bool OnOptimizedInspectorGUI(Rect contentRect)
		{
			Debug.LogError("Not supported");
			return false;
		}

		public void Repaint()
		{
			InspectorWindow.RepaintAllInspectors();
		}

		public virtual bool HasPreviewGUI()
		{
			return this.preview.HasPreviewGUI();
		}

		public virtual GUIContent GetPreviewTitle()
		{
			return this.preview.GetPreviewTitle();
		}

		public virtual Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
		{
			return null;
		}

		public virtual void OnPreviewGUI(Rect r, GUIStyle background)
		{
			this.preview.OnPreviewGUI(r, background);
		}

		public virtual void OnInteractivePreviewGUI(Rect r, GUIStyle background)
		{
			this.OnPreviewGUI(r, background);
		}

		public virtual void OnPreviewSettings()
		{
			this.preview.OnPreviewSettings();
		}

		public virtual string GetInfoString()
		{
			return this.preview.GetInfoString();
		}

		internal virtual void OnAssetStoreInspectorGUI()
		{
		}

		public virtual void ReloadPreviewInstances()
		{
			this.preview.ReloadPreviewInstances();
		}

		internal static bool DoDrawDefaultInspector(SerializedObject obj)
		{
			EditorGUI.BeginChangeCheck();
			obj.Update();
			SerializedProperty iterator = obj.GetIterator();
			bool enterChildren = true;
			while (iterator.NextVisible(enterChildren))
			{
				using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
				{
					EditorGUILayout.PropertyField(iterator, true, new GUILayoutOption[0]);
				}
				enterChildren = false;
			}
			obj.ApplyModifiedProperties();
			return EditorGUI.EndChangeCheck();
		}

		internal bool DoDrawDefaultInspector()
		{
			return Editor.DoDrawDefaultInspector(this.serializedObject);
		}

		public void DrawHeader()
		{
			if (EditorGUIUtility.hierarchyMode)
			{
				this.DrawHeaderFromInsideHierarchy();
			}
			else
			{
				this.OnHeaderGUI();
			}
		}

		protected virtual void OnHeaderGUI()
		{
			Editor.DrawHeaderGUI(this, this.targetTitle);
		}

		internal virtual void OnHeaderControlsGUI()
		{
			GUILayoutUtility.GetRect(10f, 10f, 16f, 16f, EditorStyles.layerMaskField);
			GUILayout.FlexibleSpace();
			bool flag = true;
			if (!(this is AssetImporterInspector))
			{
				if (!AssetDatabase.IsMainAsset(this.targets[0]))
				{
					flag = false;
				}
				AssetImporter atPath = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(this.targets[0]));
				if (atPath && atPath.GetType() != typeof(AssetImporter))
				{
					flag = false;
				}
			}
			if (flag)
			{
				if (GUILayout.Button("Open", EditorStyles.miniButton, new GUILayoutOption[0]))
				{
					if (this is AssetImporterInspector)
					{
						AssetDatabase.OpenAsset((this as AssetImporterInspector).assetEditor.targets);
					}
					else
					{
						AssetDatabase.OpenAsset(this.targets);
					}
					GUIUtility.ExitGUI();
				}
			}
		}

		internal virtual void OnHeaderIconGUI(Rect iconRect)
		{
			if (Editor.s_Styles == null)
			{
				Editor.s_Styles = new Editor.Styles();
			}
			Texture2D texture2D = null;
			if (!this.HasPreviewGUI())
			{
				bool flag = AssetPreview.IsLoadingAssetPreview(this.target.GetInstanceID());
				texture2D = AssetPreview.GetAssetPreview(this.target);
				if (!texture2D)
				{
					if (flag)
					{
						this.Repaint();
					}
					texture2D = AssetPreview.GetMiniThumbnail(this.target);
				}
			}
			if (this.HasPreviewGUI())
			{
				this.OnPreviewGUI(iconRect, Editor.s_Styles.inspectorBigInner);
			}
			else if (texture2D)
			{
				GUI.Label(iconRect, texture2D, Editor.s_Styles.centerStyle);
			}
		}

		internal virtual void OnHeaderTitleGUI(Rect titleRect, string header)
		{
			titleRect.yMin -= 2f;
			titleRect.yMax += 2f;
			GUI.Label(titleRect, header, EditorStyles.largeLabel);
		}

		internal virtual void DrawHeaderHelpAndSettingsGUI(Rect r)
		{
			UnityEngine.Object target = this.target;
			Vector2 vector = EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.titleSettingsIcon);
			float num = vector.x;
			Rect position = new Rect(r.xMax - num, r.y + 5f, vector.x, vector.y);
			if (EditorGUI.DropdownButton(position, EditorGUI.GUIContents.titleSettingsIcon, FocusType.Passive, EditorStyles.iconButton))
			{
				EditorUtility.DisplayObjectContextMenu(position, this.targets, 0);
			}
			num += vector.x;
			EditorGUI.HelpIconButton(new Rect(r.xMax - num, r.y + 5f, vector.x, vector.y), target);
		}

		private void DrawHeaderFromInsideHierarchy()
		{
			GUIStyle style = GUILayoutUtility.topLevel.style;
			EditorGUILayout.EndVertical();
			this.OnHeaderGUI();
			EditorGUILayout.BeginVertical(style, new GUILayoutOption[0]);
		}

		internal static Rect DrawHeaderGUI(Editor editor, string header)
		{
			return Editor.DrawHeaderGUI(editor, header, 0f);
		}

		internal static Rect DrawHeaderGUI(Editor editor, string header, float leftMargin)
		{
			if (Editor.s_Styles == null)
			{
				Editor.s_Styles = new Editor.Styles();
			}
			GUILayout.BeginHorizontal(Editor.s_Styles.inspectorBig, new GUILayoutOption[0]);
			GUILayout.Space(38f);
			GUILayout.BeginVertical(new GUILayoutOption[0]);
			GUILayout.Space(19f);
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			if (leftMargin > 0f)
			{
				GUILayout.Space(leftMargin);
			}
			if (editor)
			{
				editor.OnHeaderControlsGUI();
			}
			else
			{
				EditorGUILayout.GetControlRect(new GUILayoutOption[0]);
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			Rect lastRect = GUILayoutUtility.GetLastRect();
			Rect r = new Rect(lastRect.x + leftMargin, lastRect.y, lastRect.width - leftMargin, lastRect.height);
			Rect rect = new Rect(r.x + 6f, r.y + 6f, 32f, 32f);
			if (editor)
			{
				editor.OnHeaderIconGUI(rect);
			}
			else
			{
				GUI.Label(rect, AssetPreview.GetMiniTypeThumbnail(typeof(UnityEngine.Object)), Editor.s_Styles.centerStyle);
			}
			Rect rect2 = new Rect(r.x + 44f, r.y + 6f, r.width - 44f - 38f - 4f, 16f);
			if (editor)
			{
				editor.OnHeaderTitleGUI(rect2, header);
			}
			else
			{
				GUI.Label(rect2, header, EditorStyles.largeLabel);
			}
			if (editor)
			{
				editor.DrawHeaderHelpAndSettingsGUI(r);
			}
			Event current = Event.current;
			if (editor != null && current.type == EventType.MouseDown && current.button == 1 && r.Contains(current.mousePosition))
			{
				EditorUtility.DisplayObjectContextMenu(new Rect(current.mousePosition.x, current.mousePosition.y, 0f, 0f), editor.targets, 0);
				current.Use();
			}
			return lastRect;
		}

		public virtual void DrawPreview(Rect previewArea)
		{
			ObjectPreview.DrawPreview(this, previewArea, this.targets);
		}

		internal bool CanBeExpandedViaAFoldout()
		{
			if (this.m_SerializedObject == null)
			{
				this.m_SerializedObject = new SerializedObject(this.targets, this.m_Context);
			}
			else
			{
				this.m_SerializedObject.Update();
			}
			this.m_SerializedObject.inspectorMode = this.m_InspectorMode;
			SerializedProperty iterator = this.m_SerializedObject.GetIterator();
			bool enterChildren = true;
			bool result;
			while (iterator.NextVisible(enterChildren))
			{
				if (EditorGUI.GetPropertyHeight(iterator, null, true) > 0f)
				{
					result = true;
					return result;
				}
				enterChildren = false;
			}
			result = false;
			return result;
		}

		internal static bool IsAppropriateFileOpenForEdit(UnityEngine.Object assetObject)
		{
			string text;
			return Editor.IsAppropriateFileOpenForEdit(assetObject, out text);
		}

		internal static bool IsAppropriateFileOpenForEdit(UnityEngine.Object assetObject, out string message)
		{
			message = string.Empty;
			bool result;
			if (AssetDatabase.IsNativeAsset(assetObject))
			{
				if (!AssetDatabase.IsOpenForEdit(assetObject, out message, StatusQueryOptions.UseCachedIfPossible))
				{
					result = false;
					return result;
				}
			}
			else if (AssetDatabase.IsForeignAsset(assetObject))
			{
				if (!AssetDatabase.IsMetaFileOpenForEdit(assetObject, out message, StatusQueryOptions.UseCachedIfPossible))
				{
					result = false;
					return result;
				}
			}
			result = true;
			return result;
		}

		internal virtual bool IsEnabled()
		{
			UnityEngine.Object[] targets = this.targets;
			int i = 0;
			bool result;
			while (i < targets.Length)
			{
				UnityEngine.Object @object = targets[i];
				if ((@object.hideFlags & HideFlags.NotEditable) != HideFlags.None)
				{
					result = false;
				}
				else
				{
					if (!EditorUtility.IsPersistent(@object) || Editor.IsAppropriateFileOpenForEdit(@object))
					{
						i++;
						continue;
					}
					result = false;
				}
				return result;
			}
			result = true;
			return result;
		}

		internal bool IsOpenForEdit()
		{
			string text;
			return this.IsOpenForEdit(out text);
		}

		internal bool IsOpenForEdit(out string message)
		{
			message = "";
			UnityEngine.Object[] targets = this.targets;
			bool result;
			for (int i = 0; i < targets.Length; i++)
			{
				UnityEngine.Object @object = targets[i];
				if (EditorUtility.IsPersistent(@object) && !Editor.IsAppropriateFileOpenForEdit(@object))
				{
					result = false;
					return result;
				}
			}
			result = true;
			return result;
		}

		public virtual bool UseDefaultMargins()
		{
			return true;
		}

		public void Initialize(UnityEngine.Object[] targets)
		{
			throw new InvalidOperationException("You shouldn't call Initialize for Editors");
		}

		public bool MoveNextTarget()
		{
			this.referenceTargetIndex++;
			return this.referenceTargetIndex < this.targets.Length;
		}

		public void ResetTarget()
		{
			this.referenceTargetIndex = 0;
		}
	}
}
