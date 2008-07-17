﻿// Protocol Buffers - Google's data interchange format
// Copyright 2008 Google Inc.
// http://code.google.com/p/protobuf/
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Google.ProtocolBuffers.Descriptors;

namespace Google.ProtocolBuffers {
  /// <summary>
  /// Implementation of the non-generic IMessage interface as far as possible.
  /// </summary>
  public abstract class AbstractMessage : IMessage {
    // TODO(jonskeet): Cleaner to use a Nullable<int>?
    /// <summary>
    /// The serialized size if it's already been computed, or -1
    /// if we haven't computed it yet.
    /// </summary>
    private int memoizedSize = -1;

    #region Unimplemented members of IMessage
    public abstract MessageDescriptor DescriptorForType { get; }
    public abstract IDictionary<FieldDescriptor, object> AllFields { get; }
    public abstract bool HasField(FieldDescriptor field);
    public abstract object this[FieldDescriptor field] { get; }
    public abstract int GetRepeatedFieldCount(FieldDescriptor field);
    public abstract object this[FieldDescriptor field, int index] { get; }
    public abstract UnknownFieldSet UnknownFields { get; }
    // FIXME
    IMessage IMessage.DefaultInstanceForType { get { return null; } }
    IBuilder IMessage.CreateBuilderForType() { return null; }
    #endregion

    public bool IsInitialized {
      get {
        // Check that all required fields are present.
        foreach (FieldDescriptor field in DescriptorForType.Fields) {
          if (field.IsRequired && !HasField(field)) {
            return false;
          }
        }

        // Check that embedded messages are initialized.
        foreach (KeyValuePair<FieldDescriptor, object> entry in AllFields) {
          FieldDescriptor field = entry.Key;
          if (field.MappedType == MappedType.Message) {
            if (field.IsRepeated) {
              // We know it's an IList<T>, but not the exact type - so
              // IEnumerable is the best we can do. (C# generics aren't covariant yet.)
              foreach (IMessage element in (IEnumerable)entry.Value) {
                if (!element.IsInitialized) {
                  return false;
                }
              }
            } else {
              if (!((IMessage)entry.Value).IsInitialized) {
                return false;
              }
            }
          }
        }
        return true;
      }
    }

    public sealed override string ToString() {
      return TextFormat.PrintToString(this);
    }

    public void WriteTo(CodedOutputStream output) {
      foreach (KeyValuePair<FieldDescriptor, object> entry in AllFields) {
        FieldDescriptor field = entry.Key;
        if (field.IsRepeated) {
          // We know it's an IList<T>, but not the exact type - so
          // IEnumerable is the best we can do. (C# generics aren't covariant yet.)
          foreach (object element in (IEnumerable)entry.Value) {
            output.WriteField(field.FieldType, field.FieldNumber, element);
          }
        } else {
          output.WriteField(field.FieldType, field.FieldNumber, entry.Value);
        }
      }

      UnknownFieldSet unknownFields = UnknownFields;
      if (DescriptorForType.Options.IsMessageSetWireFormat) {
        unknownFields.WriteAsMessageSetTo(output);
      } else {
        unknownFields.WriteTo(output);
      }
    }

    public int SerializedSize {
      get {
        int size = memoizedSize;
        if (size != -1) {
          return size;
        }

        size = 0;
        foreach (KeyValuePair<FieldDescriptor, object> entry in AllFields) {
          FieldDescriptor field = entry.Key;
          if (field.IsRepeated) {
            foreach (object element in (IEnumerable)entry.Value) {
              size += CodedOutputStream.ComputeFieldSize(field.FieldType, field.FieldNumber, element);
            }
          } else {
            size += CodedOutputStream.ComputeFieldSize(field.FieldType, field.FieldNumber, entry.Value);
          }
        }

        UnknownFieldSet unknownFields = UnknownFields;
        if (DescriptorForType.Options.IsMessageSetWireFormat) {
          size += unknownFields.SerializedSizeAsMessageSet;
        } else {
          size += unknownFields.SerializedSize;
        }

        memoizedSize = size;
        return size;
      }
    }

    public ByteString ToByteString() {
      ByteString.CodedBuilder output = new ByteString.CodedBuilder(SerializedSize);
      WriteTo(output.CodedOutput);
      return output.Build();
    }

    public byte[] ToByteArray() {
      byte[] result = new byte[SerializedSize];
      CodedOutputStream output = CodedOutputStream.CreateInstance(result);
      WriteTo(output);
      output.CheckNoSpaceLeft();
      return result;
    }

    public void WriteTo(Stream output) {
      CodedOutputStream codedOutput = CodedOutputStream.CreateInstance(output);
      WriteTo(codedOutput);
      codedOutput.Flush();
    }

    public override bool Equals(object other) {
      if (other == this) {
        return true;
      }
      IMessage otherMessage = other as IMessage;
      if (otherMessage == null || otherMessage.DescriptorForType != DescriptorForType) {
        return false;
      }
      // TODO(jonskeet): Check that dictionaries support equality appropriately
      // (I suspect they don't!)
      return AllFields.Equals(otherMessage.AllFields);
    }

    public override int GetHashCode() {
      int hash = 41;
      hash = (19 * hash) + DescriptorForType.GetHashCode();
      hash = (53 * hash) + AllFields.GetHashCode();
      return hash;
    }

    #region IMessage Members

    MessageDescriptor IMessage.DescriptorForType {
      get { throw new NotImplementedException(); }
    }

    IDictionary<FieldDescriptor, object> IMessage.AllFields {
      get { throw new NotImplementedException(); }
    }

    bool IMessage.HasField(FieldDescriptor field) {
      throw new NotImplementedException();
    }

    object IMessage.this[FieldDescriptor field] {
      get { throw new NotImplementedException(); }
    }

    int IMessage.GetRepeatedFieldCount(FieldDescriptor field) {
      throw new NotImplementedException();
    }

    object IMessage.this[FieldDescriptor field, int index] {
      get { throw new NotImplementedException(); }
    }

    UnknownFieldSet IMessage.UnknownFields {
      get { throw new NotImplementedException(); }
    }

    bool IMessage.IsInitialized {
      get { throw new NotImplementedException(); }
    }

    void IMessage.WriteTo(CodedOutputStream output) {
      throw new NotImplementedException();
    }

    int IMessage.SerializedSize {
      get { throw new NotImplementedException(); }
    }

    bool IMessage.Equals(object other) {
      throw new NotImplementedException();
    }

    int IMessage.GetHashCode() {
      throw new NotImplementedException();
    }

    string IMessage.ToString() {
      throw new NotImplementedException();
    }

    ByteString IMessage.ToByteString() {
      throw new NotImplementedException();
    }

    byte[] IMessage.ToByteArray() {
      throw new NotImplementedException();
    }

    void IMessage.WriteTo(Stream output) {
      throw new NotImplementedException();
    }

    #endregion
  }
}