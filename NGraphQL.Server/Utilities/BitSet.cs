using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Utilities {
  
  public interface IBitSet {
    bool GetValue(int index);
    void SetValue(int index, bool value);
  }

  // generic version for bit count > 64
  public class BitSet: IBitSet {
    BitSet64[] _bitSets;

    public static IBitSet Create(int bitCount) {
      if (bitCount <= 64)
        return new BitSet64();
      else
        return new BitSet(bitCount); 
    }
    
    public BitSet(int bitCount) {
      _bitSets = new BitSet64[(bitCount - 1) / 64 + 1];
      for (int i = 0; i < _bitSets.Length; i++)
        _bitSets[i] = new BitSet64(); 
    }

    public bool GetValue(int index) {
      return _bitSets[index / 64].GetValue(index % 64);
    }

    public void SetValue(int index, bool value) {
      _bitSets[index / 64].SetValue(index % 64, value);
    }

  }

  // compact version for bit count <= 64
  public class BitSet64 : IBitSet {
    long _value; 

    public bool GetValue(int index) {
      return ((_value >> index) & 1L) != 0; 
    }

    public void SetValue(int index, bool value) {
      if (value)
        _value |= 1L << index;
      else
        _value &= ~(1L << index); 
    }

  }
}
