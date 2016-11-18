/******************************************************************************
 * Spine Runtimes Software License v2.5
 *
 * Copyright (c) 2013-2016, Esoteric Software
 * All rights reserved.
 *
 * You are granted a perpetual, non-exclusive, non-sublicensable, and
 * non-transferable license to use, install, execute, and perform the Spine
 * Runtimes software and derivative works solely for personal or internal
 * use. Without the written permission of Esoteric Software (see Section 2 of
 * the Spine Software License Agreement), you may not (a) modify, translate,
 * adapt, or develop new applications using the Spine Runtimes or otherwise
 * create derivative works or improvements of the Spine Runtimes or (b) remove,
 * delete, alter, or obscure any trademarks or any copyright, trademark, patent,
 * or other intellectual property or proprietary rights notices on or in the
 * Software, including any copy thereof. Redistributions in binary or source
 * form must include this license and terms.
 *
 * THIS SOFTWARE IS PROVIDED BY ESOTERIC SOFTWARE "AS IS" AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO
 * EVENT SHALL ESOTERIC SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES, BUSINESS INTERRUPTION, OR LOSS OF
 * USE, DATA, OR PROFITS) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
 * IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

using System;
using UnityEngine;
using UnityEditor;

public class SpineSpriteShaderGUI : ShaderGUI {
	static readonly string kShaderVertexLit = "Spine/Sprite/Vertex Lit";
	static readonly string kShaderPixelLit = "Spine/Sprite/Pixel Lit";
	static readonly string kShaderUnlit = "Spine/Sprite/Unlit";
	static readonly int kSolidQueue = 2000;
	static readonly int kAlphaTestQueue = 2450;
	static readonly int kTransparentQueue = 3000;

	enum eBlendMode {
		PreMultipliedAlpha,
		StandardAlpha,
		Solid,
		Additive,
		SoftAdditive,
		Multiply,
		Multiplyx2,
	}

	enum eLightMode { VertexLit, PixelLit, Unlit, }

	enum eCulling {
		Off = 0,
		Back = 2,
		Front = 1,
	}

	MaterialProperty _mainTexture = null;
	MaterialProperty _color = null;
	MaterialProperty _blendMode = null;

	MaterialProperty _emissionMap = null;
	MaterialProperty _emissionColor = null;
	MaterialProperty _emissionPower = null;

	MaterialProperty _writeToDepth = null;
	MaterialProperty _depthAlphaCutoff = null;
	MaterialProperty _shadowAlphaCutoff = null;
	MaterialProperty _renderQueue = null;
	MaterialProperty _culling = null;

	MaterialProperty _overlayColor = null;
	MaterialProperty _hue = null;
	MaterialProperty _saturation = null;
	MaterialProperty _brightness = null;

	MaterialProperty _rimPower = null;
	MaterialProperty _rimColor = null;

	MaterialEditor _materialEditor;

	//Normals
	MaterialProperty _bumpMap = null;
	MaterialProperty _diffuseRamp = null;
	MaterialProperty _fixedNormal = null;

	//Blend texture
	MaterialProperty _blendTexture = null;
	MaterialProperty _blendTextureLerp = null;
	
	bool _firstTimeApply = true;
	eLightMode _lightMode;

	#region ShaderGUI
	public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] properties)	{
		FindProperties(properties); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
		_materialEditor = materialEditor;
		Material material = materialEditor.target as Material;

		ShaderPropertiesGUI(material);

		// Make sure that needed keywords are set up if we're switching some existing
		// material to a standard shader.
		if (_firstTimeApply) {
			SetMaterialKeywords(material);
			SetLightModeFromShader(material);
			_firstTimeApply = false;
		}
	}

	public override void AssignNewShaderToMaterial (Material material, Shader oldShader, Shader newShader) {
		base.AssignNewShaderToMaterial(material, oldShader, newShader);
		SetMaterialKeywords(material);
		SetLightModeFromShader(material);
	}
	#endregion

	#region Virtual Interface
	protected virtual void FindProperties(MaterialProperty[] props) {
		_mainTexture = FindProperty("_MainTex", props);
		_color = FindProperty("_Color", props);
		_blendMode = FindProperty("_BlendMode", props);

		_emissionMap = FindProperty("_EmissionMap", props, false);
		_emissionColor = FindProperty("_EmissionColor", props, false);
		_emissionPower = FindProperty("_EmissionPower", props, false);		

		_writeToDepth = FindProperty("_ZWrite", props);
		_depthAlphaCutoff = FindProperty("_Cutoff", props);
		_shadowAlphaCutoff = FindProperty("_ShadowAlphaCutoff", props);
		_renderQueue = FindProperty("_RenderQueue", props);
		_culling = FindProperty("_Cull", props);

		_bumpMap = FindProperty("_BumpMap", props, false);
		_diffuseRamp = FindProperty("_DiffuseRamp", props, false);
		_fixedNormal = FindProperty("_FixedNormal", props, false);
		_blendTexture = FindProperty("_BlendTex", props, false);
		_blendTextureLerp = FindProperty("_BlendAmount", props, false);

		_overlayColor = FindProperty("_OverlayColor", props, false);
		_hue = FindProperty("_Hue", props, false);
		_saturation = FindProperty("_Saturation", props, false);
		_brightness = FindProperty("_Brightness", props, false);

		_rimPower = FindProperty("_RimPower", props, false);
		_rimColor = FindProperty("_RimColor", props, false);
	}

	protected virtual void ShaderPropertiesGUI(Material material) {
		// Use default labelWidth
		EditorGUIUtility.labelWidth = 0f;

		// Detect any changes to the material
		EditorGUI.BeginChangeCheck();
		{
			//GUILayout.Label("Rendering", EditorStyles.boldLabel);
			{
				RenderModes(material);
			}

			GUILayout.Label("Main Maps", EditorStyles.boldLabel);
			{
				RenderTextureProperties(material);
			}

			GUILayout.Label("Depth", EditorStyles.boldLabel);
			{
				RenderDepthProperties(material);
			}

			if (_fixedNormal != null)
			{
				GUILayout.Label("Normals", EditorStyles.boldLabel);
				RenderNormalsProperties(material);
			}

			GUILayout.Label("Shadows", EditorStyles.boldLabel);
			{
				RenderShadowsProperties(material);
			}

			GUILayout.Label("Color Adjustment", EditorStyles.boldLabel);
			{
				RenderColorProperties(material);
			}

			if (_emissionMap != null && _emissionColor != null)
			{
				GUILayout.Label("Emission", EditorStyles.boldLabel);
				{
					RenderEmissionProperties(material);
				}
			}

			if (_rimColor != null)
			{
				GUILayout.Label("Rim Lighting", EditorStyles.boldLabel);
				RenderRimLightingProperties(material);
			}

			GUILayout.Label("Fog", EditorStyles.boldLabel);
			{
				RenderFogProperties(material);
			}
		}

		if (EditorGUI.EndChangeCheck())
		{
			foreach (var obj in _blendMode.targets)
				MaterialChanged((Material)obj);
		}
	}

	protected virtual void RenderModes (Material material) {
		LightingModePopup();
		BlendModePopup();

		EditorGUI.BeginChangeCheck();
		int renderQueue = EditorGUILayout.IntSlider("Renderer Queue", (int)_renderQueue.floatValue, 0, 49);
		if (EditorGUI.EndChangeCheck())
		{
			material.SetInt("_RenderQueue", renderQueue);
		}

		EditorGUI.BeginChangeCheck();
		eCulling culling = (eCulling)Mathf.RoundToInt(_culling.floatValue);
		culling = (eCulling)EditorGUILayout.EnumPopup("Culling", culling);
		if (EditorGUI.EndChangeCheck())
		{
			material.SetInt("_Cull", (int)culling);
		}
	}

	protected virtual void RenderTextureProperties (Material material) {
		_materialEditor.TexturePropertySingleLine(new GUIContent("Albedo"), _mainTexture, _color);

		if (_bumpMap != null)
			_materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), _bumpMap);

		if (_diffuseRamp != null)
			_materialEditor.TexturePropertySingleLine(new GUIContent("Diffuse Ramp", "A black and white gradient can be used to create a 'Toon Shading' effect."), _diffuseRamp);

		if (_blendTexture != null)
		{
			EditorGUI.BeginChangeCheck();
			_materialEditor.TexturePropertySingleLine(new GUIContent("Blend Texture", "When a blend texture is set the albedo will be a mix of the blend texture and main texture based on the blend amount."), _blendTexture, _blendTextureLerp);
			if (EditorGUI.EndChangeCheck())
			{
				SetKeyword(material, "_TEXTURE_BLEND", _blendTexture != null);
			}
		}

		_materialEditor.TextureScaleOffsetProperty(_mainTexture);
	}
	
	protected virtual void RenderEmissionProperties (Material material) {
		bool emission = material.IsKeywordEnabled("_EMISSION");

		EditorGUI.BeginChangeCheck();
		emission = EditorGUILayout.Toggle("Enable Emission", emission);
		if (EditorGUI.EndChangeCheck())
		{
			SetKeyword(material, "_EMISSION", emission);
		}

		if (emission)
		{
			_materialEditor.TexturePropertyWithHDRColor(new GUIContent("Emission"), _emissionMap, _emissionColor, new ColorPickerHDRConfig(0,1, 0.01010101f, 3), true);
			_materialEditor.FloatProperty(_emissionPower, "Emission Power");				
		}
	}

	protected virtual void RenderDepthProperties (Material material) {
		EditorGUI.BeginChangeCheck();
		bool writeTodepth = EditorGUILayout.Toggle(new GUIContent("Write to Depth", "Write to Depth Buffer by clipping alpha."), _writeToDepth.floatValue != 0.0f);
		if (EditorGUI.EndChangeCheck())
		{
			material.SetInt("_ZWrite", writeTodepth ? 1 : 0);
		}

		if (writeTodepth)
		{
			_materialEditor.RangeProperty(_depthAlphaCutoff, "Depth Alpha Cutoff");
		}
	}

	protected virtual void RenderNormalsProperties (Material material) {
		EditorGUI.BeginChangeCheck();
		bool fixedNormals = material.IsKeywordEnabled("_FIXED_NORMALS");
		bool fixedNormalsBackRendering = material.IsKeywordEnabled("_FIXED_NORMALS_BACK_RENDERING");

		bool meshNormals = EditorGUILayout.Toggle(new GUIContent("Use Mesh Normals", "If this is unticked instead of requiring mesh normals a Fixed Normal will be used instead (it's quicker and can result in better looking lighting effects on 2d objects)."), 
													!fixedNormals && !fixedNormalsBackRendering);
		if (EditorGUI.EndChangeCheck())
		{
			SetKeyword(material, "_FIXED_NORMALS", meshNormals ? false : fixedNormalsBackRendering ? false : true);
			SetKeyword(material, "_FIXED_NORMALS_BACK_RENDERING", meshNormals ? false : fixedNormalsBackRendering);
		}

		if (!meshNormals)
		{
			Vector3 normal = EditorGUILayout.Vector3Field(new GUIContent("Fixed Normal", "Defined in Camera Space. Should normally be (0,0,-1)."), _fixedNormal.vectorValue);
			_fixedNormal.vectorValue = new Vector4(normal.x, normal.y, normal.z, 1.0f);

			EditorGUI.BeginChangeCheck();

			

			bool backRendering = EditorGUILayout.Toggle(new GUIContent("Fixed Normal Back Rendering", "Tick only if you are going to rotate the sprite to face away from the camera, the fixed normal will be flipped to compensate."), 
														material.IsKeywordEnabled("_FIXED_NORMALS_BACK_RENDERING"));
			if (EditorGUI.EndChangeCheck())
			{
				SetKeyword(material, "_FIXED_NORMALS_BACK_RENDERING", backRendering);
				SetKeyword(material, "_FIXED_NORMALS", !backRendering);
			}
		}
	}

	protected virtual void RenderShadowsProperties (Material material) {
		_materialEditor.FloatProperty(_shadowAlphaCutoff, "Shadow Alpha Cutoff");
	}

	protected virtual void RenderColorProperties (Material material) {
		EditorGUI.BeginChangeCheck();
		bool colorAdjust = EditorGUILayout.Toggle("Enable Color Adjustment", material.IsKeywordEnabled("_COLOR_ADJUST"));
		if (EditorGUI.EndChangeCheck())
		{
			SetKeyword(material, "_COLOR_ADJUST", colorAdjust);	
		}

		if (colorAdjust) {
			_materialEditor.ColorProperty(_overlayColor, "Overlay Color");
			_materialEditor.RangeProperty(_hue, "Hue");
			_materialEditor.RangeProperty(_saturation, "Saturation");
			_materialEditor.RangeProperty(_brightness, "Brightness");
		}
	}

	protected virtual void RenderRimLightingProperties (Material material) {
		EditorGUI.BeginChangeCheck();
		bool rimLighting = EditorGUILayout.Toggle("Enable Rim Lighting", material.IsKeywordEnabled("_RIM_LIGHTING"));
		if (EditorGUI.EndChangeCheck())
		{
			SetKeyword(material, "_RIM_LIGHTING", rimLighting);
		}

		if (rimLighting)
		{
			_materialEditor.ColorProperty(_rimColor, "Rim Color");
			_materialEditor.FloatProperty(_rimPower, "Rim Power");
		}
	}

	protected virtual void RenderFogProperties (Material material) {
		EditorGUI.BeginChangeCheck();
		bool fog = EditorGUILayout.Toggle("Enable Fog", material.IsKeywordEnabled("_FOG"));
		if (EditorGUI.EndChangeCheck())
		{
			SetKeyword(material, "_FOG", fog);
		}
	}
	#endregion

	void SetLightModeFromShader (Material material) {
		if (material.shader.name == kShaderPixelLit)
		{
			_lightMode = eLightMode.PixelLit;
		}
		else if (material.shader.name == kShaderUnlit)
		{
			_lightMode = eLightMode.Unlit;
		}
		else
		{
			_lightMode = eLightMode.VertexLit;
		}
	}

	void SetShaderFromLightMode() {
		Material material = _materialEditor.target as Material;

		switch (_lightMode)
		{
			case eLightMode.VertexLit:
				if (material.shader.name != kShaderVertexLit)
					_materialEditor.SetShader(Shader.Find(kShaderVertexLit), false);
				break;
			case eLightMode.PixelLit:
				if (material.shader.name != kShaderPixelLit)
					_materialEditor.SetShader(Shader.Find(kShaderPixelLit), false);
				break;
			case eLightMode.Unlit:
				if (material.shader.name != kShaderUnlit)
					_materialEditor.SetShader(Shader.Find(kShaderUnlit), false);
				break;
		}

		MaterialChanged(material);
	}

	static void SetMaterialKeywords(Material material) {
		eBlendMode blendMode = (eBlendMode)material.GetFloat("_BlendMode");

		bool normalMap = material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap") != null;
		SetKeyword (material, "_NORMALMAP", normalMap);

		bool zWrite = material.GetFloat("_ZWrite") > 0.0f;
		bool clipAlpha = zWrite && material.GetFloat("_Cutoff") > 0.0f;
		SetKeyword(material, "_ALPHA_CLIP", clipAlpha);

		bool diffuseRamp = material.HasProperty("_DiffuseRamp") && material.GetTexture("_DiffuseRamp") != null;
		SetKeyword(material, "_DIFFUSE_RAMP", diffuseRamp);

		bool blendTexture = material.HasProperty("_BlendTex") && material.GetTexture("_BlendTex") != null;
		SetKeyword(material, "_TEXTURE_BLEND", blendTexture);

		SetKeyword(material, "_ALPHAPREMULTIPLY_ON", blendMode == eBlendMode.PreMultipliedAlpha);
		SetKeyword(material, "_MULTIPLYBLEND", blendMode == eBlendMode.Multiply);
		SetKeyword(material, "_MULTIPLYBLEND_X2", blendMode == eBlendMode.Multiplyx2);
		SetKeyword(material, "_ADDITIVEBLEND", blendMode == eBlendMode.Additive);
		SetKeyword(material, "_ADDITIVEBLEND_SOFT", blendMode == eBlendMode.SoftAdditive);

		int renderQueue;

		switch (blendMode) {
			case eBlendMode.Solid:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					material.SetOverrideTag("RenderType", "Opaque");
					renderQueue = kSolidQueue;
				}
				break;
			case eBlendMode.Additive:
				{ 
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetOverrideTag("RenderType", "Transparent");
					renderQueue = kTransparentQueue;
				}
				break;
			case eBlendMode.SoftAdditive:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
					material.SetOverrideTag("RenderType", "Transparent");
					renderQueue = kTransparentQueue;
				}			
				break;
			case eBlendMode.Multiply:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
					material.SetOverrideTag("RenderType", "Transparent");
					renderQueue = kTransparentQueue;
				}			
				break;
			case eBlendMode.Multiplyx2:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
					material.SetOverrideTag("RenderType", "Transparent");
					renderQueue = kTransparentQueue;
				}			
				break;
			case eBlendMode.PreMultipliedAlpha:
			case eBlendMode.StandardAlpha:
			default:
				{
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					material.SetOverrideTag("RenderType", zWrite ? "TransparentCutout" : "Transparent");
					renderQueue = zWrite ? kAlphaTestQueue : kTransparentQueue;
				}
				break;
		}
		
		material.renderQueue = renderQueue + material.GetInt("_RenderQueue");
	}

	static void MaterialChanged (Material material) {		
		SetMaterialKeywords(material);
	}

	static void SetKeyword (Material m, string keyword, bool state) {
		if (state)
			m.EnableKeyword (keyword);
		else
			m.DisableKeyword (keyword);
	}

	void LightingModePopup ()
	{
		EditorGUI.BeginChangeCheck();
		_lightMode = (eLightMode)EditorGUILayout.Popup("Lighting Mode", (int)_lightMode, Enum.GetNames(typeof(eLightMode)));
		if (EditorGUI.EndChangeCheck())
		{
			SetShaderFromLightMode();
		}
	}

	void BlendModePopup () {
		eBlendMode mode = (eBlendMode)_blendMode.floatValue;
		EditorGUI.BeginChangeCheck();
		mode = (eBlendMode)EditorGUILayout.Popup("Blend Mode", (int)mode, Enum.GetNames(typeof(eBlendMode)));
		if (EditorGUI.EndChangeCheck())
		{
			_materialEditor.RegisterPropertyChangeUndo("Blend Mode");
			_blendMode.floatValue = (float)mode;
		}
	}
}