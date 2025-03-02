using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum PaintDebug
{
    none,
    splatTex,
    worldPosTex,
    worldTangentTex,
    worldBinormalTex
}

public enum TextureSize
{
    //Texture16x16 = 16,
    //Texture32x32 = 32,
    Texture64x64 = 64,
    Texture128x128 = 128,
    Texture256x256 = 256,
    Texture512x512 = 512,
    Texture1024x1024 = 1024,
    Texture2048x2048 = 2048,
    Texture4096x4096 = 4096
}

public class PaintTarget : MonoBehaviour
{
    public TextureSize paintTextureSize = TextureSize.Texture1024x1024;
    public TextureSize renderTextureSize = TextureSize.Texture1024x1024;

    public bool SetupOnStart = false;
    public bool PaintAllSplats = false;

    public PaintDebug debugTexture = PaintDebug.none;

    private Camera renderCamera = null;

    private RenderTexture splatTex;
    private RenderTexture splatTexAlt;
    public Texture2D splatTexPick;

    public ComputeShader ReadPixel = null;
    private ComputeBuffer outputBuffer;
    private int kernelID = -1;

    // 12.13 SSC
    // 페인트 초기화시 컬러값 초기화로 돌릴 origin값 저장 필드 추가
    public Texture2D originTex;

    private bool bPickDirty = true;
    private bool validTarget = false;
    private bool bHasMeshCollider = false;

    private RenderTexture worldPosTex;
    private RenderTexture worldPosTexTemp;
    private RenderTexture worldTangentTex;
    private RenderTexture worldBinormalTex;

    private List<Paint> m_Splats = new List<Paint>();
    private bool evenFrame = false;
    private bool setupComplete = false;

    private Renderer paintRenderer;

    private Material paintBlitMaterial;
    private Material worldPosMaterial;
    private Material worldTangentMaterial;
    private Material worldBiNormalMaterial;

    private static RenderTexture RT256;
    private static RenderTexture RT4;
    private static Texture2D Tex4;

    private static GameObject splatObject;
    private bool isPainted = false;

    // 12.13 SSC
    // 페인트 초기화시 컬러값 초기화로 돌릴 origin값 저장 필드 추가
    private void Awake()
    {
        ReadPixel = null;
        originTex = splatTexPick;
    }


    public static Color CursorColor()
    {
        if (Camera.main == null)
        {
            Debug.Log("Warning: No Main Camera tagged");
            return Color.black;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return RayColor(ray);
    }

    public static int CursorChannel()
    {
        if (Camera.main == null)
        {
            Debug.Log("Warning: No Main Camera tagged");
            return -1;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return RayChannel(ray);
    }

    public static int RayChannel(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 10000))
        {
            PaintTarget paintTarget = hit.collider.gameObject.GetComponent<PaintTarget>();
            if (!paintTarget) return -1;
            if (!paintTarget.validTarget) return -1;
            if (!paintTarget.bHasMeshCollider) return -1;

            Renderer r = paintTarget.GetComponent<Renderer>();
            if (!r) return -1;

            RenderTexture rt = (RenderTexture)r.sharedMaterial.GetTexture("_SplatTex");
            if (!rt) return -1;

            UpdatePickColors(paintTarget, rt);

            Texture2D tc = paintTarget.splatTexPick;
            if (!tc) return -1;


            int x = (int)(hit.textureCoord2.x * tc.width);
            int y = (int)(hit.textureCoord2.y * tc.height);

            Color pc = tc.GetPixel(x, y);

            int l = -1;
            if (pc.r > .3) l = 0;
            if (pc.g > .5) l = 1;
            if (pc.b > .5) l = 2;
            if (pc.a > .5) l = 3;

            return l;
        }

        return -1;
    }



    public static int RayChannel(Ray ray, Ray subRay, float Length, LayerMask layer)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Length, layer) || Physics.Raycast(subRay, out hit, Length*2, layer))
        {
            PaintTarget paintTarget = hit.collider.gameObject.GetComponent<PaintTarget>();
            if (!paintTarget) return -1;
            if (!paintTarget.validTarget) return -1;
            if (!paintTarget.bHasMeshCollider) return -1;

            Renderer r = paintTarget.GetComponent<Renderer>();
            if (!r) return -1;

            RenderTexture rt = (RenderTexture)r.sharedMaterial.GetTexture("_SplatTex");
            if (!rt) return -1;

            UpdatePickColors(paintTarget, rt);

            Texture2D tc = paintTarget.splatTexPick;
            if (!tc) return -1;

            int x = (int)(hit.textureCoord2.x * tc.width);
            int y = (int)(hit.textureCoord2.y * tc.height);


            //Color pc = GetColorByComputeShader(paintTarget, x, y);
            Color pc = tc.GetPixel(x, y);

            int l = -1;
            if (pc.r > .3) l = 0;
            if (pc.g > .5) l = 1;
            if (pc.b > .5) l = 2;
            if (pc.a > .5) l = 3;


            return l;
        }

        //Debug.Break();
        return -1;
    }

    public static int RayChannel(Ray ray, float Length, LayerMask layer)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Length, layer))
        {
            PaintTarget paintTarget = hit.collider.gameObject.GetComponent<PaintTarget>();            
            if (!paintTarget) return -1;
            if (!paintTarget.validTarget) return -1;
            if (!paintTarget.bHasMeshCollider) return -1;

            Renderer r = paintTarget.GetComponent<Renderer>();           
            if (!r) return -1;

            RenderTexture rt = (RenderTexture)r.sharedMaterial.GetTexture("_SplatTex");            
            if (!rt) return -1;

            UpdatePickColors(paintTarget, rt);

            Texture2D tc = paintTarget.splatTexPick;            
            if (!tc) return -1;

            int x = (int)(hit.textureCoord2.x * tc.width);
            int y = (int)(hit.textureCoord2.y * tc.height);


            //Color pc = GetColorByComputeShader(paintTarget, x, y);
            Color pc = tc.GetPixel(x, y);            

            int l = -1;
            if (pc.r > .3) l = 0;
            if (pc.g > .5) l = 1;
            if (pc.b > .5) l = 2;
            if (pc.a > .5) l = 3;


            return l;
        }

        //Debug.Break();
        return -1;
    }

    public static Color RayColor(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 10000))
        {
            PaintTarget paintTarget = hit.collider.gameObject.GetComponent<PaintTarget>();
            if (!paintTarget) return Color.black;
            if (!paintTarget.validTarget) return Color.black;
            if (!paintTarget.bHasMeshCollider) return Color.black;

            Renderer r = paintTarget.GetComponent<Renderer>();
            if (!r) return Color.black;

            RenderTexture rt = (RenderTexture)r.sharedMaterial.GetTexture("_SplatTex");
            if (!rt) return Color.black;

            UpdatePickColors(paintTarget, rt);

            Texture2D tc = paintTarget.splatTexPick;
            if (!tc) return Color.black;


            int x = (int)(hit.textureCoord2.x * tc.width);
            int y = (int)(hit.textureCoord2.y * tc.height);

            //Color pc = GetColorByComputeShader(paintTarget, x, y);
            Color pc = tc.GetPixel(x, y);
            Debug.Log(paintTarget.name + " : " + x + ", " + y + "(" + pc + ")");

            Color c1 = r.sharedMaterial.GetColor("_SplatColor1");
            Color c2 = r.sharedMaterial.GetColor("_SplatColor2");

            Color cc = Color.black;
            if (pc.r > .4) cc = c1;
            if (pc.g > .5) cc = c2;

            return cc;
        }

        return Color.black;
    }

    private static Color GetColorByComputeShader(PaintTarget paintTarget, int x, int y)
    {
        // Set the input texture and output buffer to the shader

        if(!paintTarget.ReadPixel) return Color.black;

        paintTarget.ReadPixel.SetInt("frame", paintTarget.evenFrame ? 1 : 0);
        paintTarget.ReadPixel.SetInt("coord_x", x);
        paintTarget.ReadPixel.SetInt("coord_y", y);

        // Dispatch the compute shader
        paintTarget.ReadPixel.Dispatch(paintTarget.kernelID, 1, 1, 1);

        // Read the result from the output buffer
        float[] outputArray = new float[4];
        paintTarget.outputBuffer.GetData(outputArray);

        // Extract the color components from the output array
        Color pc = new Color(outputArray[0], outputArray[1], outputArray[2], outputArray[3]);
        return pc;
    }


    public static Color RayColor(RaycastHit hit)
    {
        PaintTarget paintTarget = hit.collider.gameObject.GetComponent<PaintTarget>();
        if (!paintTarget) return Color.black;
        if (!paintTarget.validTarget) return Color.black;
        if (!paintTarget.bHasMeshCollider) return Color.black;

        Renderer r = paintTarget.GetComponent<Renderer>();
        if (!r) return Color.black;

        RenderTexture rt = (RenderTexture)r.sharedMaterial.GetTexture("_SplatTex");
        if (!rt) return Color.black;

        UpdatePickColors(paintTarget, rt);

        Texture2D tc = paintTarget.splatTexPick;
        if (!tc) return Color.black;


        int x = (int)(hit.textureCoord2.x * tc.width);
        int y = (int)(hit.textureCoord2.y * tc.height);

        Color pc = tc.GetPixel(x, y);

        Color c1 = r.sharedMaterial.GetColor("_SplatColor1");
        Color c2 = r.sharedMaterial.GetColor("_SplatColor2");

        Color cc = Color.black;
        if (pc.r > .4) cc = c1;
        if (pc.g > .5) cc = c2;

        if(cc==null) return Color.black;
        return cc;
    }

    public static Color RayColor(Ray ray, float distance, LayerMask layer)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, distance, layer))
        {
            PaintTarget paintTarget = hit.collider.gameObject.GetComponent<PaintTarget>();
            if (!paintTarget) return Color.black;
            if (!paintTarget.validTarget) return Color.black;
            if (!paintTarget.bHasMeshCollider) return Color.black;

            Renderer r = paintTarget.GetComponent<Renderer>();
            if (!r) return Color.black;

            RenderTexture rt = (RenderTexture)r.sharedMaterial.GetTexture("_SplatTex");
            if (!rt) return Color.black;

            UpdatePickColors(paintTarget, rt);

            Texture2D tc = paintTarget.splatTexPick;
            if (!tc) return Color.black;


            int x = (int)(hit.textureCoord2.x * tc.width);
            int y = (int)(hit.textureCoord2.y * tc.height);

            Color pc = tc.GetPixel(x, y);

            Color c1 = r.sharedMaterial.GetColor("_SplatColor1");
            Color c2 = r.sharedMaterial.GetColor("_SplatColor2");

            Color cc = Color.black;
            if (pc.r > .4) cc = c1;
            if (pc.g > .5) cc = c2;

            return cc;
        }

        return Color.black;
    }

    public static void PaintLine(Vector3 start, Vector3 end, Brush brush)
    {
        Ray ray = new Ray(start, (end - start).normalized);
        PaintRaycast(ray, brush);
    }

    public static void PaintRay(Ray ray, Brush brush, float range, bool multiple = true)
    {
        PaintRaycast(ray, brush, range, multiple);
    }
    public static void PaintRay(Ray ray, Brush brush, LayerMask layer, float range, bool multiple = true)
    {
        PaintRaycast(ray, brush, layer, range, multiple);
    }

    public static void PaintCursor(Brush brush)
    {
        if (Camera.main == null)
        {
            Debug.Log("Warning: No Main Camera tagged");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        PaintRaycast(ray, brush);
    }

    const float SPHERE_RADIUS = 0.5f;
    private static void PaintRaycast(Ray ray, Brush brush, float range = 50, bool multi = true)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, range))
        {
            if (multi)
            {
                // 12.27 PSC : 최적화 위해 SphereCastAll 범위 줄였음
                RaycastHit[] hits = Physics.SphereCastAll(hit.point, SPHERE_RADIUS, ray.direction, hit.distance- SPHERE_RADIUS*0.5f);
                for (int h = 0; h < hits.Length; h++)
                {
                    PaintTarget paintTarget = hits[h].collider.gameObject.GetComponent<PaintTarget>();
                    if (paintTarget != null)
                    {
                        PaintObject(paintTarget, hit.point, hits[h].normal, brush);
                        paintTarget.isPainted = true;
                    }
                }
            }
            else
            {
                PaintTarget paintTarget = hit.collider.gameObject.GetComponent<PaintTarget>();
                if (!paintTarget) return;
                PaintObject(paintTarget, hit.point, hit.normal, brush);
                paintTarget.isPainted = true;
            }
        }
    }

    private static void PaintRaycast(Ray ray, Brush brush, LayerMask layer, float range = 50, bool multi = true)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, range, layer))
        {

            if (EffectManager.instance)
                EffectManager.instance.PlayEffect(EffectList.GunExplosion, hit.point, Quaternion.identity);

            if (multi)
            {
                // LEGACY
                //RaycastHit[] hits = Physics.SphereCastAll(hit.point, brush.splatScale, ray.direction, hit.distance);
                //RaycastHit[] hitsNpc = Physics.SphereCastAll(hit.point, brush.splatScale, ray.direction, hit.distance);

                // 12.27 PSC : 최적화 위해 SphereCastAll 범위 줄였음
                RaycastHit[] hits = Physics.SphereCastAll(hit.point, SPHERE_RADIUS, ray.direction, hit.distance- SPHERE_RADIUS * 0.5f);
                RaycastHit[] hitsNpc = Physics.SphereCastAll(hit.point, SPHERE_RADIUS, ray.direction, hit.distance- SPHERE_RADIUS * 0.5f);

                for (int h=0; h < hits.Length; h++)
                {
                    PaintTarget paintTarget = hits[h].collider.gameObject.GetComponent<PaintTarget>();                    
                    if (paintTarget != null)
                    {

                        PaintObject(paintTarget, hit.point, hits[h].normal, brush);

                         // 12.26 SSC : 페인트칠시 NPC 범위로 체크하기 위하여 조건문 추가
                        for(int i = 0; i < hitsNpc.Length; i++)
                        {
                            NpcBase currNpc = hitsNpc[i].collider.gameObject.GetComponent<NpcBase>();
                            if (currNpc != null)
                            {
                                currNpc.ChangedState(npcState.glued);
                                GunStateController.AddList(currNpc);                                
                            }

                        }
                        // 12.27 SSC : 페인트칠시 GunStateController에서 HashSet으로 관리하기위해 추가
                        GunStateController.AddList(paintTarget);
                        paintTarget.isPainted = true;
                    }
                }
            }
            else
            {
                PaintTarget paintTarget = hit.collider.gameObject.GetComponent<PaintTarget>();
                if (!paintTarget) return;

                PaintObject(paintTarget, hit.point, hit.normal, brush);
                
                // 12.27 SSC : 페인트칠시 GunStateController에서 HashSet으로 관리하기위해 추가
                GunStateController.AddList(paintTarget);
                paintTarget.isPainted = true;

                // 12.26 SSC : 페인트칠시 NPC 범위로 체크하기 위하여 조건문 추가
                if (paintTarget.gameObject.GetComponent<NpcBase>() != null)
                {
                    paintTarget.gameObject.GetComponent<NpcBase>().ChangedState(npcState.glued);
                    GunStateController.AddList(paintTarget.gameObject.GetComponent<NpcBase>());                    
                }

            }
        }
    }

    public static void PaintObject(PaintTarget target, Vector3 point, Vector3 normal, Brush brush)
    {
        if (!target) return;
        if (!target.validTarget) return;

        if (splatObject == null)
        {
            splatObject = new GameObject();
            splatObject.name = "splatObject";
            splatObject.hideFlags = HideFlags.HideInHierarchy;
        }
        splatObject.transform.position = point;

        Vector3 leftVec = Vector3.Cross(normal, Vector3.up);
        if (leftVec.magnitude > 0.001f)
            splatObject.transform.rotation = Quaternion.LookRotation(leftVec, normal);
        else
            splatObject.transform.rotation = Quaternion.identity;

        float randScale = Random.Range(brush.splatRandomScaleMin, brush.splatRandomScaleMax);
        splatObject.transform.RotateAround(point, normal, brush.splatRotation);
        splatObject.transform.RotateAround(point, normal, Random.Range(-brush.splatRandomRotation, brush.splatRandomRotation));
        splatObject.transform.localScale = new Vector3(randScale, randScale, randScale) * brush.splatScale;

        Paint newPaint = new Paint();
        newPaint.paintMatrix = splatObject.transform.worldToLocalMatrix;
        newPaint.channelMask = brush.getMask();
        newPaint.scaleBias = brush.getTile();
        newPaint.brush = brush;

        if (!target.splatTexPick)
        {
            target.splatTexPick = new Texture2D((int)target.paintTextureSize, (int)target.paintTextureSize, TextureFormat.ARGB32, false);
        }


        target.PaintSplat(newPaint);
    }

    public static void ClearAllPaint()
    {
        PaintTarget[] targets = GameObject.FindObjectsOfType<PaintTarget>() as PaintTarget[];

        foreach (PaintTarget target in targets)
        {
            if (!target.validTarget) continue;
            target.ClearPaint();

            // 12.13 SSC
            // 페인트 초기화시 컬러값 초기화로 돌릴 origin값 저장 필드 추가
            target.splatTexPick = target.originTex;
        }


    }

    private static void UpdatePickColors(PaintTarget paintTarget, RenderTexture rt)
    {
        if (!paintTarget.validTarget) return;
        if (!paintTarget.bPickDirty) return;
        if (!paintTarget.bHasMeshCollider) return;

        if (!paintTarget.splatTexPick)
        {
            return;
           // paintTarget.splatTexPick = new Texture2D((int)paintTarget.paintTextureSize, (int)paintTarget.paintTextureSize, TextureFormat.ARGB32, false);
        }

        Rect rectReadPicture = new Rect(0, 0, rt.width, rt.height);
        RenderTexture.active = rt;
        paintTarget.splatTexPick.ReadPixels(rectReadPicture, 0, 0);
        paintTarget.splatTexPick.Apply();
        RenderTexture.active = null;

        paintTarget.bPickDirty = false;
    }

    private void CreateCamera()
    {
        GameObject cam = GameObject.Find("PaintCamera");
        if (cam != null)
        {
            renderCamera = cam.GetComponent<Camera>();
            return;
        }

        GameObject rtCameraObject = new GameObject();
        rtCameraObject.name = "PaintCamera";
        //rtCameraObject.tag = "PaintCamera";
        rtCameraObject.transform.position = Vector3.zero;
        rtCameraObject.transform.rotation = Quaternion.identity;
        rtCameraObject.transform.localScale = Vector3.one;
        rtCameraObject.hideFlags = HideFlags.HideInHierarchy;
        renderCamera = rtCameraObject.AddComponent<Camera>();
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = new Color(0, 0, 0, 0);
        renderCamera.orthographic = true;
        renderCamera.nearClipPlane = 0.0f;
        renderCamera.farClipPlane = 1.0f;
        renderCamera.orthographicSize = 1.0f;
        renderCamera.aspect = 1.0f;
        renderCamera.useOcclusionCulling = false;
        renderCamera.enabled = false;
        renderCamera.cullingMask = LayerMask.NameToLayer("Nothing");
    }

    void CheckValid()
    {
        paintRenderer = this.GetComponent<Renderer>();
        if (!paintRenderer) return;

        foreach (Material mat in paintRenderer.sharedMaterials)
        {
            if (!mat.shader.name.StartsWith("Paint/"))
            {
                return;
            }
        }

        validTarget = true;

        MeshCollider mc = this.GetComponent<MeshCollider>();
        if (mc != null) bHasMeshCollider = true;
    }

    private void Start()
    {
        CheckValid();

        if (SetupOnStart) SetupPaint();
    }

    private void SetupPaint()
    {
        CreateCamera();
        CreateMaterials();
        CreateTextures();

        RenderTextures();
        CreateComputeShader();

        setupComplete = true;
    }


    private void CreateComputeShader()
    {
        return;
        /*Debug.Log(ReadPixel);

        ReadPixel = Instantiate((ComputeShader)Resources.Load("Shader/ReadPixel"));

        kernelID = ReadPixel.FindKernel("ReadPixelAtCoordinates");
        outputBuffer = new ComputeBuffer(1, sizeof(float) * 4);
        ReadPixel.SetBuffer(kernelID, "outputBuffer", outputBuffer);

        ReadPixel.SetTexture(kernelID, "inputTextureEven", splatTex);
        ReadPixel.SetTexture(kernelID, "inputTextureOdd", splatTexAlt);*/
    }

    private void CreateMaterials()
    {        
        paintBlitMaterial = new Material(Shader.Find("Hidden/PaintBlit"));
        worldPosMaterial = new Material(Shader.Find("Hidden/PaintPos"));
        worldTangentMaterial = new Material(Shader.Find("Hidden/PaintTangent"));
        worldBiNormalMaterial = new Material(Shader.Find("Hidden/PaintBinormal"));
    }

    private void CreateTextures()
    {
        splatTex = new RenderTexture((int)paintTextureSize, (int)paintTextureSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        splatTex.Create();
        splatTexAlt = new RenderTexture((int)paintTextureSize, (int)paintTextureSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        splatTexAlt.Create();

        //splatTexPick = new Texture2D((int)paintTextureSize, (int)paintTextureSize, TextureFormat.ARGB32, false);
        //splatTexPick = new Texture2D((int)renderTextureSize, (int)renderTextureSize, TextureFormat.ARGB32, false);

        worldPosTex = new RenderTexture((int)renderTextureSize, (int)renderTextureSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        worldPosTex.Create();
        worldPosTexTemp = new RenderTexture((int)renderTextureSize, (int)renderTextureSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        worldPosTexTemp.Create();
        worldTangentTex = new RenderTexture((int)renderTextureSize, (int)renderTextureSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        worldTangentTex.Create();
        worldBinormalTex = new RenderTexture((int)renderTextureSize, (int)renderTextureSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        worldBinormalTex.Create();

        foreach (Material mat in paintRenderer.materials)
        {
            mat.SetTexture("_SplatTex", splatTex);
            mat.SetTexture("_WorldPosTex", worldPosTex);
            mat.SetTexture("_WorldTangentTex", worldTangentTex);
            mat.SetTexture("_WorldBinormalTex", worldBinormalTex);
            mat.SetVector("_SplatTexSize", new Vector4((int)paintTextureSize, (int)paintTextureSize, 0, 0));
        }
    }

    private void RenderTextures()
    {
        //Debug.Log("RenderTextures");
        this.transform.hasChanged = false;

        CommandBuffer cb = new CommandBuffer();

        cb.SetRenderTarget(worldPosTex);
        cb.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
        for (int i = 0; i < paintRenderer.materials.Length; i++)
        {
            cb.DrawRenderer(paintRenderer, worldPosMaterial, i);
        }

        cb.SetRenderTarget(worldTangentTex);
        cb.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
        for (int i = 0; i < paintRenderer.materials.Length; i++)
        {
            cb.DrawRenderer(paintRenderer, worldTangentMaterial, i);
        }

        cb.SetRenderTarget(worldBinormalTex);
        cb.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
        for (int i = 0; i < paintRenderer.materials.Length; i++)
        {
            cb.DrawRenderer(paintRenderer, worldBiNormalMaterial, i);
        }

        // Only have to render the camera once!
        renderCamera.AddCommandBuffer(CameraEvent.AfterEverything, cb);
        renderCamera.Render();
        renderCamera.RemoveAllCommandBuffers();

        // Bleed the world position out 2 pixels
        paintBlitMaterial.SetVector("_SplatTexSize", new Vector2((int)renderTextureSize, (int)renderTextureSize));
        Graphics.Blit(worldPosTex, worldPosTexTemp, paintBlitMaterial, 2);
        Graphics.Blit(worldPosTexTemp, worldPosTex, paintBlitMaterial, 2);

        switch (debugTexture)
        {
            case PaintDebug.splatTex:
                paintRenderer.material.SetTexture("_MainTex", splatTex);
                break;

            case PaintDebug.worldPosTex:
                paintRenderer.material.SetTexture("_MainTex", worldPosTex);
                break;

            case PaintDebug.worldTangentTex:
                paintRenderer.material.SetTexture("_MainTex", worldTangentTex);
                break;

            case PaintDebug.worldBinormalTex:
                paintRenderer.material.SetTexture("_MainTex", worldBinormalTex);
                break;
        }
    }

    public void ClearPaint()
    {
        m_Splats.Clear();
        isPainted = false;
        if (setupComplete)
        {
            CommandBuffer cb = new CommandBuffer();
            cb.SetRenderTarget(splatTex);
            cb.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
            cb.SetRenderTarget(splatTexAlt);
            cb.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));

            renderCamera.AddCommandBuffer(CameraEvent.AfterEverything, cb);
            renderCamera.Render();
            renderCamera.RemoveAllCommandBuffers();
            RemoveComputeShader();
        }
    }

    private void RemoveComputeShader()
    {
        if (ReadPixel)
        {
            outputBuffer?.Release();
            Destroy(ReadPixel);
            ReadPixel = null;
        }
    }

    public void PaintSplat(Paint paint)
    {
        m_Splats.Add(paint);

        // 12.27 SSC : 최적화 관련 탐색중
        //Debug.Log("리스트 크기 : " + m_Splats.Count);
        //PaintSplats();
        return;
    }

    private void PaintSplats()
    {
        if (!validTarget) return;

        if (m_Splats.Count > 0)
        {
            bPickDirty = true;

            if (!setupComplete) SetupPaint();

            if (this.transform.hasChanged) RenderTextures();

            if (!ReadPixel) CreateComputeShader();

            Matrix4x4[] SplatMatrixArray = new Matrix4x4[10];
            Vector4[] SplatScaleBiasArray = new Vector4[10];
            Vector4[] SplatChannelMaskArray = new Vector4[10];

            // Render up to 10 splats per frame of the same texture!
            int i = 0;
            Texture2D splatTexture = m_Splats[0].brush.splatTexture;

            for (int s=0; s < m_Splats.Count;)
            {
                if (i >= 10) break;
                if (m_Splats[s].brush.splatTexture == splatTexture)
                {
                    SplatMatrixArray[i] = m_Splats[s].paintMatrix;
                    SplatScaleBiasArray[i] = m_Splats[s].scaleBias;
                    SplatChannelMaskArray[i] = m_Splats[s].channelMask;
                    i++;
                    m_Splats.RemoveAt(s);
                }
                else
                {
                    //different texture..skip for now
                    s++;
                }
            }

            paintBlitMaterial.SetVector("_SplatTexSize", new Vector2((int)paintTextureSize, (int)paintTextureSize));
            paintBlitMaterial.SetMatrixArray("_SplatMatrix", SplatMatrixArray);
            paintBlitMaterial.SetVectorArray("_SplatScaleBias", SplatScaleBiasArray);
            paintBlitMaterial.SetVectorArray("_SplatChannelMask", SplatChannelMaskArray);

            paintBlitMaterial.SetInt("_TotalSplats", i);

            paintBlitMaterial.SetTexture("_WorldPosTex", worldPosTex);

            // Ping pong between the buffers to properly blend splats.
            // If this were a compute shader you could just update one buffer.
            if (evenFrame)
            {
                paintBlitMaterial.SetTexture("_LastSplatTex", splatTexAlt);
                Graphics.Blit(splatTexture, splatTex, paintBlitMaterial, 0);

                foreach (Material mat in paintRenderer.materials)
                {
                    mat.SetTexture("_SplatTex", splatTex);
                }
                evenFrame = false;
            }
            else
            {
                paintBlitMaterial.SetTexture("_LastSplatTex", splatTex);
                Graphics.Blit(splatTexture, splatTexAlt, paintBlitMaterial, 0);
                foreach (Material mat in paintRenderer.materials)
                {
                    mat.SetTexture("_SplatTex", splatTexAlt);
                }

                evenFrame = true;
            }
        }
    }

    private void Update()
    {
        if (PaintAllSplats)
        {
            while (m_Splats.Count > 0)
            {
                PaintSplats();
            }
        }
        else
            PaintSplats();
    }

    void OnDestroy()
    {
        // Release the output buffer
        outputBuffer?.Release();
    }

    public bool CheckPainted()
    {
        return isPainted;
    }

}