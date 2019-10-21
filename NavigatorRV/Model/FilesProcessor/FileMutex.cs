using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NavigatorRV.Model
{
    /// <summary>
    /// Класс осуществляет разделенный доступ к файлам при их поиске
    /// </summary>
    public class FileSystemMutex
    {
        protected FileSystemMutex() { }
        protected static readonly FileSystemMutex _instance = new FileSystemMutex();
        private Mutex _fileMutex = new Mutex();

        /// <summary>
        /// Выполняет код делегата внутри потокобезопасно (используется мьютекс, для выполнения кода)
        /// </summary>
        /// <param name="a">Функция (делегат)</param>
        /// <param name="args">Параметры</param>
        public void Enter(Delegate a, object[] args = null)
        {
            Wait();
            a.DynamicInvoke(args);
            Release();
        }

        protected void Wait()
        {
            _fileMutex.WaitOne();
        }

        protected void Release()
        {
            _fileMutex.ReleaseMutex();
        }

        public static FileSystemMutex Instance { get { return _instance; } }
    }
}
