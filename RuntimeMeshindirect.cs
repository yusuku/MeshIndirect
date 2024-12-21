using System.Runtime.InteropServices;
using UnityEngine;

public class RuntimeMeshIndirect : MonoBehaviour
{
    public Material material; // �`��Ɏg�p����}�e���A��
    public Mesh mesh; // �`��Ɏg�p���郁�b�V��

    private GraphicsBuffer commandBuf;
    private GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;

    public RenderTexture inputTexture; // ���̓e�N�X�`��
    private GraphicsBuffer _positionBuffer;
    private GraphicsBuffer _colorBuffer;

    public Material debugMaterial; // �f�o�b�O�p�}�e���A��
    private RenderTexture outputTexture;

    private int _kernelId;
    private int width, height;
    private uint xThread, yThread;
    public ComputeShader computeShader;

    void Start()
    {
        // �e�N�X�`���̃T�C�Y���擾
        width = inputTexture.width;
        height = inputTexture.height;

        // �o�̓e�N�X�`���̍쐬
        outputTexture = new RenderTexture(width, height, 0)
        {
            enableRandomWrite = true
        };
        outputTexture.Create();

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
        computeShader.SetTexture(_kernelId, "Result", outputTexture);

        // �f�o�b�O�p�}�e���A���̃Z�b�g�A�b�v
        if (debugMaterial != null)
        {
            debugMaterial.mainTexture = outputTexture;
        }
    }

    void OnDestroy()
    {
        // ���\�[�X�̉��
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
        // Compute Shader �̃f�B�X�p�b�`
        computeShader.Dispatch(_kernelId, Mathf.CeilToInt(width / (float)xThread), Mathf.CeilToInt(height / (float)yThread), 1);

        // �f�o�b�O�p�}�e���A���̍X�V
        if (debugMaterial != null)
        {
            Debug.Log("ddd");
            debugMaterial.mainTexture = outputTexture;
        }

        // ���b�V���̃C���X�^���V���O�`��
        RenderParams renderParams = new RenderParams(material)
        {
            worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one), // �傫�ȃo�E���h��ݒ�
            matProps = new MaterialPropertyBlock()
        };

        renderParams.matProps.SetBuffer("_Positions", _positionBuffer);
        renderParams.matProps.SetBuffer("_Colors", _colorBuffer);

        Graphics.RenderMeshIndirect(renderParams, mesh, commandBuf);
    }
}
