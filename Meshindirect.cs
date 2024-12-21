using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

public class Meshindirect : MonoBehaviour
{
    public Material material;
    public Mesh mesh;

    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;

    public Texture2D inputTexture;

    GraphicsBuffer _positionBuffer;
    GraphicsBuffer _colorBuffer;
    public Material Debugmat;


    int width, height;
    public ComputeShader cs;
    void Start()
    {
        width=inputTexture.width; height=inputTexture.height;
        RenderTexture outputTexture = new RenderTexture(width, height, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        commandData[0].indexCountPerInstance = mesh.GetIndexCount(0);
        commandData[0].instanceCount = (uint)(width * height);

        _positionBuffer= new GraphicsBuffer(GraphicsBuffer.Target.Structured, width * height, Marshal.SizeOf<Vector3>());
        _colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,width*height,Marshal.SizeOf<Color>());

        int _kernelId =cs.FindKernel("CSMain");
        cs.SetInt("width", width);
        cs.SetInt("height", height);
        cs.GetKernelThreadGroupSizes(_kernelId, out var x, out var y, out _);
        cs.SetTexture(_kernelId, "inputTexture",inputTexture);
        cs.SetBuffer(_kernelId, "PositionResult", _positionBuffer);
        cs.SetBuffer(_kernelId, "ColorResult", _colorBuffer);
        cs.SetTexture(_kernelId, "Result", outputTexture);
        Debugmat.mainTexture = outputTexture;

        cs.Dispatch(_kernelId, Mathf.CeilToInt(width / (float)x), Mathf.CeilToInt(height / (float)y), 1);

        RenderParams rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds for better FOV culling
        rp.matProps = new MaterialPropertyBlock();
        rp.matProps.SetBuffer("_Positions", _positionBuffer);
        rp.matProps.SetBuffer("_Colors", _colorBuffer);

        commandBuf.SetData(commandData);
        Graphics.RenderMeshIndirect(rp, mesh, commandBuf);
    }

    void OnDestroy()
    {
        commandBuf?.Release();
        commandBuf = null;
    }

    void Update()
    {
        RenderParams rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds for better FOV culling
        rp.matProps = new MaterialPropertyBlock();
        rp.matProps.SetBuffer("_Positions", _positionBuffer);
        rp.matProps.SetBuffer("_Colors", _colorBuffer);

        Graphics.RenderMeshIndirect(rp, mesh, commandBuf);

    }
}
