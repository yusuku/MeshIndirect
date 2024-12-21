using System.Runtime.InteropServices;
using UnityEngine;

public class RuntimeMeshIndirect : MonoBehaviour
{
    public Material material; // 描画に使用するマテリアル
    public Mesh mesh; // 描画に使用するメッシュ

    private GraphicsBuffer commandBuf;
    private GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;

    public RenderTexture inputTexture; // 入力テクスチャ
    private GraphicsBuffer _positionBuffer;
    private GraphicsBuffer _colorBuffer;

    public Material debugMaterial; // デバッグ用マテリアル
    private RenderTexture outputTexture;

    private int _kernelId;
    private int width, height;
    private uint xThread, yThread;
    public ComputeShader computeShader;

    void Start()
    {
        // テクスチャのサイズを取得
        width = inputTexture.width;
        height = inputTexture.height;

        // 出力テクスチャの作成
        outputTexture = new RenderTexture(width, height, 0)
        {
            enableRandomWrite = true
        };
        outputTexture.Create();

        // コマンドバッファの初期化
        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        commandData[0].indexCountPerInstance = mesh.GetIndexCount(0); // メッシュのインデックス数
        commandData[0].instanceCount = (uint)(width * height); // インスタンス数
        commandBuf.SetData(commandData);

        // バッファの初期化
        _positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, width * height, Marshal.SizeOf<Vector3>());
        _colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, width * height, Marshal.SizeOf<Color>());

        // Compute Shader のセットアップ
        _kernelId = computeShader.FindKernel("CSMain");
        computeShader.SetInt("width", width);
        computeShader.SetInt("height", height);
        computeShader.GetKernelThreadGroupSizes(_kernelId, out xThread, out yThread, out _);
        computeShader.SetTexture(_kernelId, "inputTexture", inputTexture);
        computeShader.SetBuffer(_kernelId, "PositionResult", _positionBuffer);
        computeShader.SetBuffer(_kernelId, "ColorResult", _colorBuffer);
        computeShader.SetTexture(_kernelId, "Result", outputTexture);

        // デバッグ用マテリアルのセットアップ
        if (debugMaterial != null)
        {
            debugMaterial.mainTexture = outputTexture;
        }
    }

    void OnDestroy()
    {
        // リソースの解放
        commandBuf?.Release();
        commandBuf = null;

        _positionBuffer?.Release();
        _positionBuffer = null;

        _colorBuffer?.Release();
        _colorBuffer = null;

        if (outputTexture != null)
        {
            outputTexture.Release();
            outputTexture = null;
        }
    }

    void Update()
    {
        // Compute Shader のディスパッチ
        computeShader.Dispatch(_kernelId, Mathf.CeilToInt(width / (float)xThread), Mathf.CeilToInt(height / (float)yThread), 1);

        // デバッグ用マテリアルの更新
        if (debugMaterial != null)
        {
            Debug.Log("ddd");
            debugMaterial.mainTexture = outputTexture;
        }

        // メッシュのインスタンシング描画
        RenderParams renderParams = new RenderParams(material)
        {
            worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one), // 大きなバウンドを設定
            matProps = new MaterialPropertyBlock()
        };

        renderParams.matProps.SetBuffer("_Positions", _positionBuffer);
        renderParams.matProps.SetBuffer("_Colors", _colorBuffer);

        Graphics.RenderMeshIndirect(renderParams, mesh, commandBuf);
    }
}
