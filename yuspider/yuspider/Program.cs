using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;

namespace yuspider
{
    class Program
    {
        #region
        int maxNum;//又多少节点
        const int maxLevel = 4;//最下面的层编号，最上层编号为0
        const int expandNodes = 5;//每个节点扩展expandNodes-1个子节点
        string[] dataValue;
        string[] dataID;
        string[] picture;
        int[,] Matrix;
        int[] ToFriends;
        int[] ToQueue;
        int totalNumber;
        int globalIndex;
        int[,] rawMatrix;
        int diameter;
        int[] degree;
        int[,] dist;
        const int inf = 10000;
        int[] connectedCom;
        int[] visited;
        int[] comCount;
        #endregion

        public Program()
        {
            maxNum = 1;
            for (int i = 0; i <= maxLevel; ++i)
            {
                maxNum *= expandNodes;
            }

            dataValue = new string[maxNum];
            dataID = new string[maxNum];
            picture = new string[maxNum];
            ToFriends = new int[maxNum + 1];
            ToQueue = new int[maxNum + 1];
            rawMatrix = new int[maxNum + 1, maxNum + 1];
            for (int i = 0; i < maxNum; ++i)
            {
                for (int j = 0; j < maxNum; ++j) { 
                    rawMatrix[i,j] = 0;
                }
                rawMatrix[i, i] = 1;
            }
            globalIndex = -1;
        }

        string GetPage(String url)
        {
            WebClient webClient = new WebClient();
            webClient.Headers.Add("Cookie", "_r01_=1; depovince=BJ; p=; t=; societyguester=; id=123456; xnsid=; kl=kl_123456");
            Byte[] pageData = webClient.DownloadData(url);
            string pageText = Encoding.UTF8.GetString(pageData);

            return pageText;
        }

        private void Start(MatchCollection myFriendsMatch)
        {

            for (int i = 0; i < expandNodes && i < myFriendsMatch.Count; i++)
            {
                SpiderData(myFriendsMatch[i],0, -1);
            }
        }

        private int CalcuteIndex()
        {
            ++this.globalIndex;

            return this.globalIndex;
        }

        private void SpiderData(Match myFriendMatch, int level, int pID)
        {
            int currentIndex = CalcuteIndex();

            if (pID != -1)
            {
                rawMatrix[pID, currentIndex] = 1;
                rawMatrix[currentIndex, pID] = 1;
            }

            Regex regex;
            string myFriend = myFriendMatch.Value.ToString();

            regex = new Regex("portal=profileFriendlist&id=.*?\"");
            Match myFriendMatchInfo = regex.Match(myFriend);
            string myFriendInfo = myFriendMatchInfo.Value.ToString();

            regex = new Regex("id=\\d*");
            Match myFriendMatchID = regex.Match(myFriendInfo);
            string myFriendID = myFriendMatchID.Value.ToString();
            dataID[currentIndex] = myFriendID.Replace("id=", "");


            regex = new Regex("查看.*?的个人主页");
            Match myfriendmatchInfo2 = regex.Match(myFriend);
            string myfriendInfo2 = myfriendmatchInfo2.Value.ToString();
            dataValue[currentIndex] = myfriendInfo2.Replace("查看", "").Replace("的个人主页", "");

            regex = new Regex("src=\"http://.*?.jpg");
            Match myFirendMatchPicture = regex.Match(myFriend);
            string myFirendPicture = myFirendMatchPicture.Value.ToString();
            picture[currentIndex] = myFirendPicture.Replace("src=\"", "");

            //just one level
            if (level >= maxLevel)
                return;
            //fetch their friends
            string newHtml = GetPage("http://www.renren.com/profile.do?portal=profileFriendlist&id=" + dataID[currentIndex]);
            regex = new Regex("<a stats=\"pf_friend\" class=\"avatar\" href=\"http://www.renren.com/profile.do\\?portal=profileFriendlist&id=.*?\" title=\"查看.*?的个人主页\">\\n<img stats=\"pf_friend\" src=\".*?.jpg");
            MatchCollection newmyfriendsmatch = regex.Matches(newHtml);

            Console.WriteLine("level: "+ level + "  hello" + currentIndex.ToString() + " pid "+ pID);
            for (int j = 1; j < expandNodes && j < newmyfriendsmatch.Count; j++)
            {
                SpiderData(newmyfriendsmatch[j], level + 1, currentIndex);
            }
        }

        public void YuSpider()
        {
            String htmlData = GetPage("http://www.renren.com/profile.do?portal=profileFriendlist&id=238461140");
            Regex regex = new Regex("<a stats=\"pf_friend\" class=\"avatar\" href=\"http://www.renren.com/profile.do\\?portal=profileFriendlist&id=.*?\" title=\"查看.*?的个人主页\">\\n<img stats=\"pf_friend\" src=\".*?.jpg");
            Start(regex.Matches(htmlData));
        }

        public void FetchData()
        {
            PrepareFetchData(dataValue);
        }

        private void PrepareFetchData(string[] friends)
        {
            totalNumber = 0;
            int i;

            for (i = 0; i < this.globalIndex+1; i++)
            {
                if (IsSame(i, friends) == -1)
                {
                    ToFriends[totalNumber] = i;
                    ToQueue[i] = totalNumber;
                    totalNumber++;
                }
                else
                {
                    ToQueue[i] = ToQueue[IsSame(i, friends)];
                }
            }
            ToFriends[totalNumber] = this.globalIndex + 1;
            ToQueue[this.globalIndex + 1] = totalNumber;

            Matrix = new int[totalNumber + 1, totalNumber + 1];
            
            Console.WriteLine(this.totalNumber);
            TransformToMatrix(friends);
        }

        public void TransformToMatrix(string[] friends)
        {
            int i, j;
            for (i = 0; i < this.globalIndex + 1; ++i)
            {
                for (j = 0; j < this.globalIndex + 1; ++j)
                {
                    Matrix[ToQueue[i], ToQueue[j]] = 0;
                    Matrix[ToQueue[j], ToQueue[i]] = 0;
                }

                Matrix[ToQueue[i], ToQueue[i]] = 1;
            }

            for (i = 0; i < this.globalIndex + 1; ++i)
            {
                for (j = 0; j < this.globalIndex + 1; ++j)
                {
                    if (rawMatrix[i, j] == 1)
                    {
                        Matrix[ToQueue[i], ToQueue[j]] = 1;
                        Matrix[ToQueue[j], ToQueue[i]] = 1;
                    }
                }

            }

            StoreDataValue(friends);
            StorePicture(friends);
            StoreMatrix(friends);
        }

        private void StoreDataValue(string[] friends)
        {
            FileStream fs = new FileStream(@"e:\renrenwang\Friends.txt", FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.GetEncoding("GB2312"));//通过指定字符编码方式可以实现对汉字的支持，否则在用记事本打开查看会出现乱码 
            sw.Flush();
            sw.BaseStream.Seek(0, SeekOrigin.End);   //从哪里开始写入.
            for (int i = 0; i < totalNumber; i++)
            {
                sw.Write(friends[ToFriends[i]].ToString());
                sw.WriteLine();
            }

            sw.Flush();
            sw.Close();
        }

        private void StorePicture(string[] friends)
        {
            int i;
            for (i = 0; i < totalNumber; i++)
            {
                WebClient wc = new WebClient();
                wc.DownloadFile(picture[ToFriends[i]], @"e:\renrenwang\pic\" + i.ToString() + ".jpg");
            }
        }

        private void StoreMatrix(string[] friends)
        {
            int i, j;
            FileStream fs = new FileStream(@"e:\renrenwang\AM.txt", FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.GetEncoding("GB2312"));//通过指定字符编码方式可以实现对汉字的支持，否则在用记事本打开查看会出现乱码 
            sw.Flush();
            sw.BaseStream.Seek(0, SeekOrigin.End);   //从哪里开始写入.
            for (i = 0; i < this.totalNumber; i++)
            {
                for (j = 0; j < this.totalNumber; j++)
                {
                    sw.Write(Matrix[i, j].ToString() + ",");
                }
                sw.Write(Matrix[this.totalNumber, this.totalNumber].ToString());
                sw.WriteLine();
                sw.Flush();
            }
            sw.Close();
        }

        int IsSame(int i, string[] friends)
        {
            int j;
            for (j = 0; j < i; j++)
                if (friends[i] == friends[j])
                    return j;
            return -1;
        }

        private void FloydWarshall()
        {
            dist = new int[this.totalNumber+1, this.totalNumber+1];
            int i, j;
            for (i = 0; i < this.totalNumber; ++i)
            {
                for (j = 0; j < this.totalNumber; ++j)
                {
                    dist[i, j] = inf;//相当于inf
                    if (Matrix[i, j] == 1)
                        dist[i, j] = 1;
                }
                dist[i, i] = 0;
            }
            int k;
            for (k = 0; k < this.totalNumber; ++k)
            {
                for (i = 0; i < this.totalNumber; ++i)
                {
                    for (j = 0; j < this.totalNumber; ++j)
                    {
                        if (dist[i, k] == inf || dist[k, j] == inf)
                            continue;
                        if ((dist[i, j] == inf) || (dist[i, k] + dist[k, j] < dist[i, j]))
                        {
                            dist[j, i] = dist[i, j] = dist[i, k] + dist[k, j];
                        }
                    }
                }
            }
        }

        public void CalcuteDiameter()
        {
            FloydWarshall();
            int i, j;
            int ret = 0;
            for (i = 0; i < this.totalNumber; ++i)
            {
                for (j = 0; j < this.totalNumber; ++j)
                {
                    if (dist[i, j] == inf)//相当于inf
                    {
                        continue;
                    }
                    if (dist[i, j] > ret)
                        ret = dist[i, j];
                }
            }
	
            this.diameter = ret;
            StoreDiameter();
        }

        private void StoreDiameter()
        {
            FileStream fs = new FileStream(@"e:\renrenwang\Diameter.txt", FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.GetEncoding("GB2312"));//通过指定字符编码方式可以实现对汉字的支持，否则在用记事本打开查看会出现乱码 
            sw.Flush();
            sw.BaseStream.Seek(0, SeekOrigin.End);   //从哪里开始写入.
            sw.Write("diameter is:" + diameter.ToString());
            sw.WriteLine();
            sw.Flush();
            sw.Close();
        }

        public void CalcuteDegree()
        {
            int i, j;
            degree = new int[this.totalNumber+1];
	
            for (i = 0; i < this.totalNumber; ++i)
            {
                degree[i] = 0;
                for (j = 0; j < this.totalNumber; ++j)
                {
                    if (Matrix[i, j] == 1)
                    {
                        ++degree[i];
                    }
                }

            }

            StoreDegree();
        }

        private void StoreDegree()
        {
            FileStream fs = new FileStream(@"e:\renrenwang\Degree.txt", FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.GetEncoding("GB2312"));//通过指定字符编码方式可以实现对汉字的支持，否则在用记事本打开查看会出现乱码 
            sw.Flush();
            sw.BaseStream.Seek(0, SeekOrigin.End);   //从哪里开始写入.
            for (int i = 0; i < totalNumber; i++)
            {
                sw.Write(dataValue[i].ToString() + ": " + degree[i].ToString());
                sw.WriteLine();
            }

            sw.Flush();
            sw.Close();
        }

        public void CalcuteConnectedCom()
        {
            int i;
            connectedCom = new int[this.totalNumber+2];
            visited = new int[this.totalNumber+1];
            comCount = new int[this.totalNumber + 1];
            for (i = 0; i < this.totalNumber; ++i)
            {
                visited[i] = 0;
            }
            connectedCom[0] = 0;//计算有多少个强连通分支
            for (i = 0; i < this.totalNumber; ++i)
            {
                if (visited[i] == 0)
                {
                    comCount[++connectedCom[0]] = 0;
                    VisitTree(i, connectedCom[0]);
                }
            }

            StoreConnectedCom();
        }

        private void VisitTree(int visitedNum, int count)
        {
            int i, j;
            if (visited[visitedNum] == 1)
                return;
            ++comCount[count];
            visited[visitedNum] = 1;
            for (i = 0; i < this.totalNumber; ++i)
            {
                if (visited[i]==0 && Matrix[visitedNum, i] == 1)
                {
                    VisitTree(i, count);
                }
            }
        }

        private void StoreConnectedCom()
        {
            FileStream fs = new FileStream(@"e:\renrenwang\ConnectedCom.txt", FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.GetEncoding("GB2312"));//通过指定字符编码方式可以实现对汉字的支持，否则在用记事本打开查看会出现乱码 
            sw.Flush();
            sw.BaseStream.Seek(0, SeekOrigin.End);   //从哪里开始写入.
            for (int i = 1; i <= connectedCom[0]; i++)
            {
                sw.Write("connectcom:" + i.ToString() + " has" + comCount[i].ToString() +"node(s)");
                sw.WriteLine();
            }

            sw.Flush();
            sw.Close();
        }

        static void Main(string[] args)
        {
            Program app = new Program();
            app.YuSpider();
            app.FetchData();
            app.CalcuteDiameter();
            app.CalcuteDegree();
            app.CalcuteConnectedCom();
            Console.ReadLine();
        }
    }
}
