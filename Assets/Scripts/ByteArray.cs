using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ByteArray
{
    /// <summary>
    /// 默认大小
    /// </summary>
    const int DEAFAULT_SIZE = 1024;

    /// <summary>
    /// 初始大小
    /// </summary>
    int initSize = 0;

    /// <summary>
    /// 缓冲区
    /// </summary>
    public byte[] bytes;

    /// <summary>
    /// 读写位置
    /// </summary>
    public int readIndex = 0;
    public int writeIndex = 0;

    /// <summary>
    /// 剩余容量
    /// </summary>
    private int capacity;

    /// <summary>
    /// 剩余空间
    /// </summary>
    public int remain
    {
        get
        {
            return capacity - writeIndex;
        }
    }

    /// <summary>
    /// 数据长度
    /// </summary>
    public int length
    {
        get
        {
            return writeIndex - readIndex;
        }
    }
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="size"></param>
    public ByteArray(int size = DEAFAULT_SIZE)
    {
        bytes = new byte[size];
        capacity = size;
        initSize = size;
        readIndex = 0;
        writeIndex = 0;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="defaultBytes"></param>
    public ByteArray(byte[] defaultBytes)
    {
        bytes = defaultBytes;
        capacity = defaultBytes.Length;
        initSize = defaultBytes.Length;
        readIndex = 0;
        writeIndex = defaultBytes.Length;
    }
    
    /// <summary>
    /// 重新设置大小
    /// </summary>
    public void ReSize(int size)
    {
        if (size < length || size <initSize) return;
        int n = 1;
        while (n < size) n *= 2;
        capacity = n;
        byte[] newBytes = new byte[capacity];

        Array.Copy(bytes, readIndex, newBytes, 0, writeIndex - readIndex);
        
        bytes = newBytes;
        writeIndex = length;
        readIndex = 0;
    }

    /// <summary>
    /// 检查并移动数据
    /// </summary>
    public void CheckAndMoveBytes()
    {
        if (length < 8)
        {
            MoveBytes();
        }
    }

    /// <summary>
    /// 移动数据
    /// </summary>
    public void MoveBytes()
    {
        if (length > 0)
        {
            Array.Copy(bytes, readIndex, bytes, 0, length);
        }
        writeIndex = length;
        readIndex = 0;
    }

    /// <summary>
    /// 写入数据
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    public int Write(byte[] b,int offset,int count)
    {
        if(remain < count)
        {
            ReSize(count + length);
        }
        Array.Copy(b, offset, bytes, writeIndex, count);
        writeIndex += count;
        return count;
    }

    /// <summary>
    /// 读取数据
    /// </summary>
    /// <param name="bs"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int Read(byte[] bs,int offset,int count)
    {
        count = Math.Min(count, length);
        Array.Copy(bytes, readIndex, bs, offset, count);
        readIndex += count;
        CheckAndMoveBytes();
        return count;
    }

    /// <summary>
    /// 读取int16
    /// </summary>
    /// <returns></returns>
    public Int16 ReadInt16()
    {
        if (length < 2) return 0;
        Int16 ret = (Int16)((bytes[readIndex + 1] << 8) | bytes[readIndex]);
        readIndex += 2;
        CheckAndMoveBytes();
        return ret;
    }

    /// <summary>
    /// 读取Int32
    /// </summary>
    /// <returns></returns>
    public Int32 ReadInt32()
    {
        if (length < 4) return 0;
        Int32 ret = (Int32)(bytes[readIndex + 3] << 24 |
            (bytes)[readIndex + 2] << 16 |
            (bytes)[readIndex + 1] << 8 |
            (bytes)[readIndex + 0]);
        readIndex += 4;
        CheckAndMoveBytes();
        return ret;
    }

    /// <summary>
    /// 打印缓冲区(仅为调试信息)
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return BitConverter.ToString(bytes, readIndex, length);
    }

    /// <summary>
    /// 打印调试信息(仅为调试信息)
    /// </summary>
    /// <returns></returns>
    public string Debug()
    {
        return string.Format("readIndex{0} writeIndex{1} bytes{2}", readIndex, writeIndex, BitConverter.ToString(bytes, 0, length));
    }

}
