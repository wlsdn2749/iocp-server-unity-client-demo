// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: Enum.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Protocol {

  /// <summary>Holder for reflection information generated from Enum.proto</summary>
  public static partial class EnumReflection {

    #region Descriptor
    /// <summary>File descriptor for Enum.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static EnumReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CgpFbnVtLnByb3RvEghQcm90b2NvbCpoCgpQbGF5ZXJUeXBlEhQKEFBMQVlF",
            "Ul9UWVBFX05PTkUQABIWChJQTEFZRVJfVFlQRV9LTklHSFQQARIUChBQTEFZ",
            "RVJfVFlQRV9NQUdFEAISFgoSUExBWUVSX1RZUEVfQVJDSEVSEAMqgwEKC0xv",
            "Z2luUmVzdWx0EhEKDUxPR0lOX1NVQ0NFU1MQABIZChVMT0dJTl9FTUFJTF9O",
            "T1RfRk9VTkQQAhIVChFMT0dJTl9QV19NSVNNQVRDSBADEhYKEkxPR0lOX1NF",
            "UlZFUl9FUlJPUhBiEhcKE0xPR0lOX0RFRkFVTFRfRVJST1IQYyp7Cg5SZWdp",
            "c3RlclJlc3VsdBIUChBSRUdJU1RFUl9TVUNDRVNTEAASHAoYUkVHSVNURVJf",
            "RFVQTElDQVRFX0VNQUlMEAESGQoVUkVHSVNURVJfU0VSVkVSX0VSUk9SEGIS",
            "GgoWUkVHSVNURVJfREVGQVVMVF9FUlJPUhBjYgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(new[] {typeof(global::Protocol.PlayerType), typeof(global::Protocol.LoginResult), typeof(global::Protocol.RegisterResult), }, null, null));
    }
    #endregion

  }
  #region Enums
  public enum PlayerType {
    [pbr::OriginalName("PLAYER_TYPE_NONE")] None = 0,
    [pbr::OriginalName("PLAYER_TYPE_KNIGHT")] Knight = 1,
    [pbr::OriginalName("PLAYER_TYPE_MAGE")] Mage = 2,
    [pbr::OriginalName("PLAYER_TYPE_ARCHER")] Archer = 3,
  }

  public enum LoginResult {
    [pbr::OriginalName("LOGIN_SUCCESS")] LoginSuccess = 0,
    /// <summary>
    /// Email 미등록
    /// </summary>
    [pbr::OriginalName("LOGIN_EMAIL_NOT_FOUND")] LoginEmailNotFound = 2,
    /// <summary>
    /// 비밀번호 불일치
    /// </summary>
    [pbr::OriginalName("LOGIN_PW_MISMATCH")] LoginPwMismatch = 3,
    /// <summary>
    /// 기타 내부 오류
    /// </summary>
    [pbr::OriginalName("LOGIN_SERVER_ERROR")] LoginServerError = 98,
    /// <summary>
    /// 기본 에러
    /// </summary>
    [pbr::OriginalName("LOGIN_DEFAULT_ERROR")] LoginDefaultError = 99,
  }

  public enum RegisterResult {
    [pbr::OriginalName("REGISTER_SUCCESS")] RegisterSuccess = 0,
    /// <summary>
    /// 이메일 중복
    /// </summary>
    [pbr::OriginalName("REGISTER_DUPLICATE_EMAIL")] RegisterDuplicateEmail = 1,
    /// <summary>
    /// 기타 내부 오류
    /// </summary>
    [pbr::OriginalName("REGISTER_SERVER_ERROR")] RegisterServerError = 98,
    /// <summary>
    /// 기본 에러
    /// </summary>
    [pbr::OriginalName("REGISTER_DEFAULT_ERROR")] RegisterDefaultError = 99,
  }

  #endregion

}

#endregion Designer generated code
