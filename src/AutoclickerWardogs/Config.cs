using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace AutoclickerWardogs
{
    [DataContract]
    class Config
    {
        [DataMember(Name = "intervaloMs")] public int IntervaloMs;
        [DataMember(Name = "tremor")] public int Tremor;
        [DataMember(Name = "saidaMs")] public int SaidaMs;
        [DataMember(Name = "saidaPx")] public int SaidaPx;
        [DataMember(Name = "autoParar")] public bool AutoParar;
        [DataMember(Name = "limiar")] public int Limiar;
        [DataMember(Name = "checagens")] public int Checagens;
        [DataMember(Name = "sempreNoTopo")] public bool SempreNoTopo;
        [DataMember(Name = "bipe")] public bool Bipe;
        [DataMember(Name = "verificarAoAbrir")] public bool VerificarAoAbrir;

        static readonly string Pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoclickerWardogs");
        static readonly string Arquivo = Path.Combine(Pasta, "config.json");

        public Config() => Padroes();

        // o serializador não chama o construtor; garante os padrões para campos ausentes no arquivo
        [OnDeserializing] void AoDesserializar(StreamingContext c) => Padroes();

        void Padroes()
        {
            IntervaloMs = 5; Tremor = 1; SaidaMs = 1000; SaidaPx = 150; AutoParar = true; Limiar = 60; Checagens = 6;
            SempreNoTopo = true; Bipe = true; VerificarAoAbrir = true;
        }

        public static Config Carregar()
        {
            try
            {
                if (File.Exists(Arquivo))
                    using (var fs = File.OpenRead(Arquivo))
                        return (Config)new DataContractJsonSerializer(typeof(Config)).ReadObject(fs);
            }
            catch { }
            return new Config();
        }

        public void Salvar()
        {
            Directory.CreateDirectory(Pasta);
            using (var fs = File.Create(Arquivo))
                new DataContractJsonSerializer(typeof(Config)).WriteObject(fs, this);
        }
    }
}
