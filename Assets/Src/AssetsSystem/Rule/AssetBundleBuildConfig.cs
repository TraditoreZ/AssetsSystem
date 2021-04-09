using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AssetSystem
{
    public class AssetBundleBuildConfig
    {
        private static Stack<AssetBundleRule> ruleStack = new Stack<AssetBundleRule>();
        private static AssetBundleRule currtRule;
        public static AssetBundleRule[] GetRules(string path)
        {
            List<AssetBundleRule> ruleList = new List<AssetBundleRule>();
            //SystemRule
            ResolveRule(ruleList, path);
            return ruleList.Count > 0 ? ruleList.ToArray() : null;
        }



        public static void ResolveRule(List<AssetBundleRule> customRule, string path)
        {
            if (path.LastIndexOf(".txt") == -1)
            {
                path += ".txt";
            }
            if (!File.Exists(path))
            {
                Debug.LogError("Not Find Path:" + path);
                return;
            }
            var strs = File.ReadAllLines(path);
            ruleStack.Clear();
            currtRule = null;
            // 进行解析
            foreach (var line in strs)
            {
                ResolveCommand(customRule, line);
            }
        }


        public static AssetBundleMatchInfo MatchAssets(string path, AssetBundleRule rule)
        {
            if (Regex.IsMatch(path, rule.expression))
            {
                var match = Regex.Match(path, rule.expression);
                string packName = rule.packName;
                if (match.Groups.Count > 1)
                {
                    string[] format = new string[match.Groups.Count - 1];
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        format[i - 1] = match.Groups[i].Value;
                    }
                    packName = string.Format(rule.packName, format);
                }
                AssetBundleMatchInfo info = new AssetBundleMatchInfo();
                info.path = path.ToLower();
                info.packName = packName.ToLower();
                info.options = rule.options;
                if (rule.subRule.Count > 0)
                {
                    AssetBundleMatchInfo subInfo = null;
                    foreach (var sub in rule.subRule)
                    {
                        subInfo = MatchAssets(path, sub);
                        if (subInfo != null)
                        {
                            return subInfo;
                        }
                    }
                }
                return info;
            }
            else
            {
                return null;
            }
        }


        private static void ResolveCommand(List<AssetBundleRule> customRule, string command)
        {
            // 去注释
            if (command.IndexOf("//") >= 0)
            {
                command = command.Substring(0, command.IndexOf("//"));
            }
            // 去空格
            command = command.Replace(" ", "");
            // 去空
            if (string.IsNullOrEmpty(command))
            {
                return;
            }

            if (command.Contains("=>"))// 命令解析
            {
                AssetBundleRule tempRule = new AssetBundleRule();
                if (ruleStack.Count == 0)
                {
                    customRule.Add(tempRule);
                }
                else
                {
                    ruleStack.Peek().subRule.Add(tempRule);
                }
                currtRule = tempRule;

                // 命令解析
                var cmds = command.Split(new string[] { "=>", ":" }, System.StringSplitOptions.RemoveEmptyEntries);
                if (cmds.Length < 2)
                {
                    Debug.LogError("ResolveCommand Error:" + command);
                    return;
                }
                // 正则表达式
                tempRule.expression = cmds[0];
                // 包名
                tempRule.packName = cmds[1];
                // 参数
                if (cmds.Length == 3)
                {
                    tempRule.options = cmds[2].Split('|');
                }
                else
                {
                    tempRule.options = new string[0];
                }
            }
            else if (command.Contains("{"))// 子命令
            {
                if (currtRule == null)
                {
                    Debug.LogError("AssetBundleBuildConfig Error");
                    return;
                }
                if (ruleStack.Count == 0 || ruleStack.Peek() != currtRule)
                {
                    ruleStack.Push(currtRule);
                }
            }
            else if (command.Contains("}"))// 子命令结束
            {
                ruleStack.Pop();
            }
        }


    }

}