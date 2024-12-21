using System.Runtime.InteropServices;
using UnityEngine;

public class RuntimeMeshIndirect : MonoBehaviour
{
    public Material material; // �`��Ɏg�p����}�e���A��
    public Mesh mesh; // �`��Ɏg�p���郁�b�V��
    public ComputeShader cs;
    public RenderTexture inputtex;
    TextureGPUInstancing draw;


    void Start()
    {
        draw = new TextureGPUInstancing(material, mesh, cs, inputtex);
    }

    void OnDestroy()
    {
        draw.Release();
    }

    void Update()
    {
        draw.DrawMeshes();
    }

    
}
public class TextureGPUInstancing
{
    Material material; // �`��Ɏg�p����}�e���A��
    Mesh mesh; // �`��Ɏg�p���郁�b�V��
    ComputeShader computeShader;
    RenderTexture inputTexture; // ���̓e�N�X�`��

    private GraphicsBuffer commandBuf;
    private GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;

    private GraphicsBuffer _positionBuffer;
    private GraphicsBuffer _colorBuffer;

    private int _kernelId;
    private int width, height;
    private uint xThread, yThread;


    public TextureGPUInstancing(Material material, Mesh mesh, ComputeShader computeShader, RenderTexture inputTexture)
    {

        this.material = material;
        this.mesh = mesh;
        this.computeShader = computeShader;
        this.inputTexture = inputTexture;

        this.width = inputTexture.width;
        this.height = inputTexture.height;

        // �R�}���h�o�b�t�@�̏�����
        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        commandData[0].indexCountPerInstance = mesh.GetIndexCount(0); // ���b�V���̃C���f�b�N�X��
        commandData[0].instanceCount = (uint)(width * height); // �C���X�^���X��
        commandBuf.SetData(commandData);

        // �o�b�t�@�̏�����
        _positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, width * height, Marshal.SizeOf<Vector3>());
        _colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, width * height, Marshal.SizeOf<Color>());

        // Compute Shader �̃Z�b�g�A�b�v
        _kernelId = computeShader.FindKernel("CSMain");
        computeShader.SetInt("width", width);
        computeShader.SetInt("height", height);
        computeShader.GetKernelThreadGroupSizes(_kernelId, out xThread, out yThread, out _);
        computeShader.SetTexture(_kernelId, "inputTexture", inputTexture);
        computeShader.SetBuffer(_kernelId, "PositionResult", _positionBuffer);
        computeShader.SetBuffer(_kernelId, "ColorResult", _colorBuffer);

    }

    public void DrawMeshes()
    {
        computeShader.Dispatch(_kernelId, Mathf.CeilToInt(width / (float)xThread), Mathf.CeilToInt(height / (float)yThread), 1);

        RenderParams renderParams = new RenderParams(material)
        {
            worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one), // �傫�ȃo�E���h��ݒ�
            matProps = new MaterialPropertyBlock()
        };

        renderParams.matProps.SetBuffer("_Positions", _positionBuffer);
        renderParams.matProps.SetBuffer("_Colors", _colorBuffer);

        Graphics.RenderMeshIndirect(renderParams, mesh, commandBuf);

    }

    public void Release()
    {
        commandBuf?.Release();
        commandBuf = null;

        _positionBuffer?.Release();
        _positionBuffer = null;

        _colorBuffer?.Release();
        _colorBuffer = null;
    }
}
