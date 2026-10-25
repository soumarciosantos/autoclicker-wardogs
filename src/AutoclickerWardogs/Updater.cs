using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AutoclickerWardogs
{
    [DataContract]
    class Novidade
    {
        [DataMember(Name = "versao")] public string Versao;
        [DataMember(Name = "data")] public string Data;
        [DataMember(Name = "itens")] public string[] Itens;
    }

    [DataContract]
    class InfoAtualizacao
    {
        [DataMember(Name = "versao")] public string Versao;
        [DataMember(Name = "exe")] public string Exe;
        [DataMember(Name = "sha256")] public string Sha256;
        [DataMember(Name = "novidades")] public Novidade[] Novidades;
    }

    static class Updater
    {
        public const string Repositorio = "https://github.com/soumarciosantos/autoclicker-wardogs";
        const string UrlPadrao = "https://raw.githubusercontent.com/soumarciosantos/autoclicker-wardogs/main/update.json";

        // AUTOCLICKER_UPDATE_URL permite testar com outro update.json (ex.: file:///...)
        static string UrlInfo => Environment.GetEnvironmentVariable("AUTOCLICKER_UPDATE_URL") ?? UrlPadrao;

        public static readonly Version Versao = new Version(Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
        public static string VersaoTexto => Versao.ToString(3);

        static string ExeAtual => Application.ExecutablePath;

        public static Novidade[] NovidadesLocais()
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("novidades.json"))
                return (Novidade[])new DataContractJsonSerializer(typeof(Novidade[])).ReadObject(s);
        }

        static WebClient NovoCliente()
        {
            var wc = new WebClient { Encoding = Encoding.UTF8 };
            wc.Headers[HttpRequestHeader.CacheControl] = "no-cache";
            wc.Headers[HttpRequestHeader.UserAgent] = "AutoclickerWardogs/" + VersaoTexto;
            return wc;
        }

        public static InfoAtualizacao Verificar()
        {
            string url = UrlInfo;
            // parâmetro extra só em http(s), para fugir do cache do GitHub
            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url += (url.Contains("?") ? "&" : "?") + "t=" + DateTime.UtcNow.Ticks;

            using (var wc = NovoCliente())
            {
                string json = wc.DownloadString(url).TrimStart('﻿');
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    return (InfoAtualizacao)new DataContractJsonSerializer(typeof(InfoAtualizacao)).ReadObject(ms);
            }
        }

        public static bool TemVersaoNova(InfoAtualizacao info) => new Version(info.Versao) > Versao;

        public static string Sha256(byte[] dados)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(dados).Select(b => b.ToString("x2")));
        }

        // Baixa o novo .exe, confere o hash, troca o arquivo e abre a nova versão.
        // O Windows não deixa sobrescrever um .exe aberto, mas deixa renomear.
        public static void Instalar(InfoAtualizacao info)
        {
            if (string.IsNullOrEmpty(info.Exe) || string.IsNullOrEmpty(info.Sha256))
                throw new Exception("o update.json não tem \"exe\" e \"sha256\"");

            byte[] dados;
            using (var wc = NovoCliente())
                dados = wc.DownloadData(info.Exe);
            if (Sha256(dados) != info.Sha256.ToLowerInvariant())
                throw new Exception("o arquivo baixado não confere com o sha256 do update.json");

            string novo = ExeAtual + ".new", velho = ExeAtual + ".old";
            File.WriteAllBytes(novo, dados);
            if (File.Exists(velho)) File.Delete(velho);
            File.Move(ExeAtual, velho);
            File.Move(novo, ExeAtual);
            Process.Start(ExeAtual, "--atualizado");
        }

        // Apaga o .old deixado pela atualização anterior (o processo antigo pode demorar a fechar)
        public static void LimparVersaoAntiga()
        {
            string velho = ExeAtual + ".old";
            if (!File.Exists(velho)) return;
            new Thread(() =>
            {
                for (int i = 0; i < 20 && File.Exists(velho); i++)
                {
                    try { File.Delete(velho); } catch { Thread.Sleep(500); }
                }
            }) { IsBackground = true }.Start();
        }
    }
}
