using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;


namespace Obi
{

[ExecuteInEditMode]
[ImageEffectAllowedInSceneView]
[RequireComponent (typeof(Camera))]
public class ObiFluidRenderer : MonoBehaviour
{

	public enum FluidMode{
		Dielectric = 0,
		Opaque = 1
	}

	public ObiParticleRenderer meshRenderer;

	public FluidMode mode = FluidMode.Dielectric;

	public Color color = Color.gray;

    public float curvatureSoftness = 4;
	[Range(0,256)]
	public int maxCurvatureIterations = 64;
	[Range(0,0.001f)]
	public float curvatureTimeStep = 0.0008f;
	[Range(0,2)]
	public float thicknessCutoff = 1.2f;

	public float thicknessScale = 1;

	[Range(-0.5f,0.5f)]
	public float refraction = 0.01f;

	[Range(0,1)]
	public float smoothness = 0.8f;
	
	private CommandBuffer renderFluid;
	private Material depth_BlurMaterial;
	private Material normal_ReconstructMaterial;
	private Material fluid_ShadingMaterial;
	private Material thickness_Material;
		
	private void Cleanup()
	{

		if (renderFluid != null)
			GetComponent<Camera>().RemoveCommandBuffer (CameraEvent.BeforeImageEffectsOpaque,renderFluid);

		if (depth_BlurMaterial != null)
			Object.DestroyImmediate (depth_BlurMaterial);
		if (normal_ReconstructMaterial != null)
			Object.DestroyImmediate (normal_ReconstructMaterial);
		if (fluid_ShadingMaterial != null)
			Object.DestroyImmediate (fluid_ShadingMaterial);
		if (thickness_Material != null)
			Object.DestroyImmediate (thickness_Material);
	}

	private static Material CreateMaterial (Shader shader)
    {
		if (!shader || !shader.isSupported)
            return null;
        Material m = new Material (shader);
        m.hideFlags = HideFlags.HideAndDontSave;
        return m;
    }
	
	private void Setup()
	{

		if (depth_BlurMaterial == null)
		{
			depth_BlurMaterial = CreateMaterial(Shader.Find("Hidden/ScreenSpaceCurvatureFlow"));
		}

		if (normal_ReconstructMaterial == null)
		{
			normal_ReconstructMaterial = CreateMaterial(Shader.Find("Hidden/NormalReconstruction"));
		}

		if (thickness_Material == null)
		{
			thickness_Material = CreateMaterial(Shader.Find("Hidden/FluidThickness"));
		}

		if (fluid_ShadingMaterial == null)
		{
			fluid_ShadingMaterial = CreateMaterial(Shader.Find("Hidden/FluidShader"));
		}

		bool shadersSupported = depth_BlurMaterial && normal_ReconstructMaterial && thickness_Material && fluid_ShadingMaterial;

		if (!shadersSupported ||
			!SystemInfo.supportsImageEffects || 
			!SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.Depth) ||
			!SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.RFloat) ||
			!SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.ARGBHalf)
 			)
        {
            enabled = false;
			Debug.LogWarning("Obi Fluid Renderer not supported in this platform.");
            return;
        }

	}
	
	public void OnEnable()
	{
		GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
		Cleanup();
	}
	
	public void OnDisable()
	{
		Cleanup();
	}

	public void SetupFluidRenderingCommandBuffer(Mesh fluidMesh,Material fluidMaterial, Matrix4x4 transform)
	{

		renderFluid.Clear();
		renderFluid.name = "Render fluid";

		if (fluidMesh == null || fluidMaterial == null)
			return;
		
		int refraction = Shader.PropertyToID("_Refraction");
		int depth = Shader.PropertyToID("_FluidDepthTexture");

		int thickness1 = Shader.PropertyToID("_FluidThickness1");
		int thickness2 = Shader.PropertyToID("_FluidThickness2");

		int surface1 = Shader.PropertyToID("_FluidSurface1");
		int surface2 = Shader.PropertyToID("_FluidSurface2");

		int normals = Shader.PropertyToID("_FluidNormals");

		renderFluid.GetTemporaryRT(refraction,-1,-1,0,FilterMode.Bilinear);
		renderFluid.GetTemporaryRT(depth,-2,-2,24,FilterMode.Bilinear,RenderTextureFormat.Depth);

		renderFluid.GetTemporaryRT(thickness1,-2,-2,0,FilterMode.Bilinear,RenderTextureFormat.RFloat);
		renderFluid.GetTemporaryRT(thickness2,-2,-2,0,FilterMode.Bilinear,RenderTextureFormat.RFloat);

		renderFluid.GetTemporaryRT(surface1,-2,-2,0,FilterMode.Bilinear,RenderTextureFormat.RFloat);
		renderFluid.GetTemporaryRT(surface2,-2,-2,0,FilterMode.Bilinear,RenderTextureFormat.RFloat);

		renderFluid.GetTemporaryRT(normals,-2,-2,0,FilterMode.Bilinear,RenderTextureFormat.ARGBHalf);
		
		// Copy screen contents to refract them later.
		renderFluid.Blit (BuiltinRenderTextureType.CurrentActive, refraction);

		renderFluid.SetRenderTarget(depth); // fluid depth
		renderFluid.ClearRenderTarget(true,true,Color.clear); //clear 
		
		// draw fluid depth texture:
		renderFluid.DrawMesh(fluidMesh,transform,fluidMaterial);

		// draw fluid thickness:
		renderFluid.SetRenderTarget(thickness1);
		renderFluid.ClearRenderTarget(true,true,Color.black); //clear color to black (0 thickness)
		renderFluid.DrawMesh(fluidMesh,transform,thickness_Material);

		// blur fluid thickness:
		renderFluid.Blit(thickness1,thickness2,thickness_Material,1);
		renderFluid.Blit(thickness2,thickness1,thickness_Material,2);
		
		// curvature flow loop:
		renderFluid.SetGlobalTexture("_FluidDepth", depth);
		renderFluid.SetGlobalFloat("_CurrentIter",0);
		renderFluid.Blit(depth,surface1,depth_BlurMaterial);

		int source = surface1;
		int destination = surface2;

		for (int i = 0; i < maxCurvatureIterations; i++){
			renderFluid.SetGlobalFloat("_CurrentIter",i+1);
			renderFluid.Blit(source,destination,depth_BlurMaterial);
			int aux = destination;
			destination = source;
			source = aux;
		}

		renderFluid.ReleaseTemporaryRT(depth);
		renderFluid.ReleaseTemporaryRT(destination);

		// reconstruct normals from smoothed depth:
		renderFluid.Blit(source,normals,normal_ReconstructMaterial);
		
		// render fluid:
		renderFluid.SetGlobalTexture("_Refraction", refraction);
		renderFluid.SetGlobalTexture("_Thickness",thickness1);
		renderFluid.SetGlobalTexture("_Normals",normals);
		renderFluid.Blit(surface1,BuiltinRenderTextureType.CameraTarget,fluid_ShadingMaterial,(int)mode);	

	}

	void OnPreRender(){

		bool act = gameObject.activeInHierarchy && enabled;
		if (!act)
		{
			Cleanup();
			return;
		}

		Setup();

	 	Camera m_Cam = GetComponent<Camera>();
		
		Shader.SetGlobalMatrix("_Camera_to_World",m_Cam.cameraToWorldMatrix);
		Shader.SetGlobalMatrix("_World_to_Camera",m_Cam.worldToCameraMatrix);
		Shader.SetGlobalMatrix("_InvProj",m_Cam.projectionMatrix.inverse);

		float fovY = m_Cam.fieldOfView;
        float far = m_Cam.farClipPlane;
        float y = 2 * Mathf.Tan (fovY * Mathf.Deg2Rad * 0.5f) * far;
        float x = y * m_Cam.aspect;
		Shader.SetGlobalVector("_FarCorner",new Vector3(x,y,far));

		depth_BlurMaterial.SetFloat("_CurvatureParam", Mathf.Max(0,curvatureSoftness) * m_Cam.pixelWidth * 0.5f * m_Cam.nearClipPlane / 2.0f);
		depth_BlurMaterial.SetFloat("_TimeStep",curvatureTimeStep);
		
		fluid_ShadingMaterial.SetColor("_TransmittedColor", color);
		fluid_ShadingMaterial.SetFloat("_ThicknessScale", thicknessScale);
		fluid_ShadingMaterial.SetFloat("_RefractionCoeff", refraction);
		fluid_ShadingMaterial.SetFloat("_ThicknessCutoff", thicknessCutoff);
		fluid_ShadingMaterial.SetFloat("_Smoothness", smoothness);

		if (renderFluid == null)
		{
			renderFluid = new CommandBuffer();
			renderFluid.name = "Render Obi Fluid";
			m_Cam.AddCommandBuffer (CameraEvent.BeforeImageEffectsOpaque, renderFluid);
		}

		SetupFluidRenderingCommandBuffer(meshRenderer.ParticleMesh,meshRenderer.GetComponent<Renderer>().sharedMaterial,meshRenderer.transform.localToWorldMatrix);

	}
}
}

