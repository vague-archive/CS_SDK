// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace Graphics
{

using global::System;
using global::System.Collections.Generic;
using global::Google.FlatBuffers;

public struct DrawCircle : IFlatbufferObject
{
  private Struct __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public void __init(int _i, ByteBuffer _bb) { __p = new Struct(_i, _bb); }
  public DrawCircle __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public Vec2 Position { get { return (new Vec2()).__assign(__p.bb_pos + 0, __p.bb); } }
  public float Z { get { return __p.bb.GetFloat(__p.bb_pos + 8); } }
  public float Radius { get { return __p.bb.GetFloat(__p.bb_pos + 12); } }
  public uint Subdivisions { get { return __p.bb.GetUint(__p.bb_pos + 16); } }
  public float Rotation { get { return __p.bb.GetFloat(__p.bb_pos + 20); } }
  public Graphics.Color Color { get { return (new Graphics.Color()).__assign(__p.bb_pos + 24, __p.bb); } }

  public static Offset<Graphics.DrawCircle> CreateDrawCircle(FlatBufferBuilder builder, float position_X, float position_Y, float Z, float Radius, uint Subdivisions, float Rotation, float color_R, float color_G, float color_B, float color_A) {
    builder.Prep(4, 40);
    builder.Prep(4, 16);
    builder.PutFloat(color_A);
    builder.PutFloat(color_B);
    builder.PutFloat(color_G);
    builder.PutFloat(color_R);
    builder.PutFloat(Rotation);
    builder.PutUint(Subdivisions);
    builder.PutFloat(Radius);
    builder.PutFloat(Z);
    builder.Prep(4, 8);
    builder.PutFloat(position_Y);
    builder.PutFloat(position_X);
    return new Offset<Graphics.DrawCircle>(builder.Offset);
  }
}


}
