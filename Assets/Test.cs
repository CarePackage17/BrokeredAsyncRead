using System;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class Test : MonoBehaviour
{
    void Start()
    {
#if ENABLE_WINMD_SUPPORT
        var libraryId = Windows.Storage.KnownLibraryId.Pictures;
        var folder = Windows.Storage.StorageLibrary.GetLibraryAsync(libraryId).AsTask().Result.SaveFolder;

        string baseDir = folder.Path;
#else
        string baseDir = Application.persistentDataPath;
#endif

        RunTest(baseDir);
    }

    void RunTest(string baseDir)
    {
        string path = Path.Combine(baseDir, "test.txt");
        if (!File.Exists(path))
        {
            Debug.Log($"Writing to {path}");
            File.WriteAllText(path, "hello this is dog");
        }

        FileHandle handle = AsyncReadManager.OpenFileAsync(path);
        handle.JobHandle.Complete();

        if (handle.Status == FileStatus.Open)
        {
            unsafe
            {
                NativeArray<byte> readBuffer = new(1024, Allocator.Temp);
                ReadCommand read = new ReadCommand() { Buffer = readBuffer.GetUnsafePtr(), Size = readBuffer.Length, Offset = 0 };
                NativeArray<ReadCommand> readCommands = new NativeArray<ReadCommand>(1, Allocator.Temp);
                readCommands[0] = read;
                ReadHandle readHandle = AsyncReadManager.Read(handle, new ReadCommandArray() { CommandCount = 1, ReadCommands = (ReadCommand*)readCommands.GetUnsafePtr() });
                readHandle.JobHandle.Complete();

                string content = Encoding.UTF8.GetString(readBuffer.ToArray());
                Debug.Log(content);
            }
        }
        else
        {
            Debug.Log($"File handle status: {handle.Status}");
        }
    }
}
