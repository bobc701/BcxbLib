using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using BCX.BCXCommon;

namespace BCX.BCXB {

   public class CTeam {

      const int SZ_BAT = 26; //We use 1..25, 0 is unused.
      const int SZ_PIT = 12; //We use 1.11, 0 is unused.
      const int SZ_AB = 2;    
      const int SZ_POS = 10;   //1..9;
      const int SZ_SLOT = 10;  //1..9;
      const int SZ_LINESCORE = 31;    //1..30; //index in line score

      public CGame g = null;
      public CBatter[] bat;  
      public CPitcher[] pit;

      public CBatRealSet lgStats;
      public string fileName;
      public string teamTag;
      public bool usesDhDefault = false;

      public string city, nick, lineName;
      
      public int[] linup = new int[SZ_SLOT];
      public int slot;
      public int[] xbox = new int[SZ_BAT];//<-- boxx For bbox entry i, tells what batter (bs_) ix he is.

      public CHittingParamSet lgMean =
         new CHittingParamSet(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0);

      public CTeam (CGame g1) {
      // -------------------------------------
      // Constructor
      // Leave g1 as null if there is no game contect.

         g = g1;
         //Don't use g's usingDh for this...
         //usingDh = g == null ? false : g.usingDh;
      }


      public string CurrentBatterName {
   // ----------------------------------
         get { return bat[linup[slot]].bname; }
      }


      public string CurrentPitcherName {
   // ----------------------------------
         get { return pit[curp].pname; }
      }


   // pitcher arrays
      public int curp;  //<--px
      public int[] ybox = new int[SZ_PIT];//<--pbx  For pbox entry i, tells what pitcher (ps_) ix he is.

   // fielding
      public int[] who = new int[SZ_POS];    //<-- bx
      

      /// <summary>
      /// TASK: Open sFile, find start of sTeam (eg, NYA2005), and then read
      /// records filling team roster variables for side, ab.
      /// </summary>
      /// -----------------------------------------------------------------
      public void ReadTeam(StreamReader f, int ab) { 

      // fMean0 is the fudge factor for hits. 
 
      // bx is index into the batter matrix, bp.
      // px is index into the pitcher matrix, pp.

         int ptr=0;
         int bx, px;
         int slot, posn;
         string rec, sVer;
         //int pitSlot=0, pitPosn=0; string pitStats = "";

         string db_NameUse, db_SkillStr, db_stats="";

         bat = new CBatter[SZ_BAT];
         pit = new CPitcher[SZ_PIT];

         // Line 1: Read the version...
            rec = f.ReadLine();
            sVer = rec.Trim();

         // Line 2:Read the team record...
            string[] aRec = (f.ReadLine()).Split(',');
            nick = aRec[3]; city = aRec[2]; lineName = aRec[1];
            teamTag = aRec[0];
            fileName = aRec[0] + ".bcxt";

         // Line 3: League-level stats...
            aRec = (f.ReadLine()).Split(',');
            db_stats = aRec[1];
            if (db_stats[0] != '0')
               throw new Exception("Error in ReadTeam: Expected '0'");
            ptr = 1;
            lgStats.ab = CBCXCommon.GetHex(db_stats, ref ptr, 5);
            lgStats.h = CBCXCommon.GetHex(db_stats, ref ptr, 4);
            lgStats.b2 = CBCXCommon.GetHex(db_stats, ref ptr, 4);
            lgStats.b3 = CBCXCommon.GetHex(db_stats, ref ptr, 3);
            lgStats.hr = CBCXCommon.GetHex(db_stats, ref ptr, 3);
            lgStats.so = CBCXCommon.GetHex(db_stats, ref ptr, 4);
            lgStats.sh = CBCXCommon.GetHex(db_stats, ref ptr, 3);
            lgStats.sf = CBCXCommon.GetHex(db_stats, ref ptr, 3);
            lgStats.bb = CBCXCommon.GetHex(db_stats, ref ptr, 4);
            lgStats.ibb = CBCXCommon.GetHex(db_stats, ref ptr, 3);
            lgStats.hbp = CBCXCommon.GetHex(db_stats, ref ptr, 3);
            lgStats.sb = CBCXCommon.GetHex(db_stats, ref ptr, 3);
            lgStats.cs = CBCXCommon.GetHex(db_stats, ref ptr, 3);
            //lgStats.ip = (double)(CBCXCommon.GetHex(db_stats, ref ptr, 5)) / 3.0;
            lgStats.ip3 = CBCXCommon.GetHex(db_stats, ref ptr, 5);
            lgStats.ave = lgStats.ab == 0 ?
               0.0 :
               Math.Round((double)lgStats.h / (double)lgStats.ab, 3);

            lgMean.FillLgParas(lgStats);
            lgMean.h /= CGame.fMean0; // This applies the fudge factor...   

         // Last character is using Dh (1=true, 0=not)...
            usesDhDefault = db_stats[db_stats.Length - 1] == '1'; //Last char is DH default.

         // Lines 4+: Player records (batter & pitcher)...
            bx = 0;
            px = 0;
            CBatter b;
            CPitcher p;
            while ((rec = f.ReadLine()) != null) {

               aRec = rec.Split(',');
               db_NameUse = aRec[0]; db_SkillStr = aRec[1]; db_stats = aRec[2];

            // First read the batter stats, for both batters & pitchers...
               ptr = 1;

            // Logic for if the game itself will use DH. Depends on home (ab=1)
            // or vis (ab=0). It is driven by home. We set g.UsingDh if ab=1.
            // Note: important to call ReadTeam(1) first, then (0).
            {   
               bool usingDh = g != null ? g.UsingDh : usesDhDefault;
                  if (g==null) {
                     usingDh = usesDhDefault;
                  }
                  else {
                     switch (ab) {
                        case 0: usingDh = g.UsingDh; break;
                        case 1: usingDh = usesDhDefault; g.UsingDh = usingDh; break;                
                     }
                  }
                  if (usingDh) ptr += 2;
                  slot = CBCXCommon.GetHex(db_stats, ref ptr, 1);
                  posn = CBCXCommon.GetHex(db_stats, ref ptr, 1);
                  if (!usingDh) ptr += 2;
               }
               bx++;
               if (bx >= SZ_BAT) throw new Exception("Too many batters in " + teamTag);
               b = bat[bx] = new CBatter(g);
               //if (bx > 25) MessageBox.Show("Too many batters in " + sTeam);
               b.bname = db_NameUse;
               b.skillStr = db_SkillStr;
               FillBatStats(db_stats, ref b.br, ref ptr);
               b.par.FillBatParas(b.br, lgMean);
               b.when = (slot == 10 ? 0 : slot);
               if (slot > 0 && slot <= 9) linup[slot] = bx; //So 10 (non-hitting pitcher) is slot 0
               b.where = (posn == 10 ? 10 : posn); //dh is 10 in the file, keep that.
               if (posn > 0 && posn <= 9) who[posn] = bx;
               b.used = (slot > 0 || posn > 0);
               b.bx = bx;
               b.px = 0; //See below, this is assigned for pitchers.
               b.sidex = (side)ab; //Tells which team he's on, 0 or 1.

               if (db_stats[0] == '2')
               {
               // It's a Pitcher record...
                  px++; //Initialized to 0, so starts with 1.
                  if (px == 1) curp = 1;  //First pitcher listed starts today.
                  if (px >= SZ_PIT) throw new Exception("Too many pitchers in " + teamTag);
                  p = pit[px] = new CPitcher();
                  b.px = px;
                  p.pname = db_NameUse;
                  FillPitStats(db_stats, ref p.pr, ref ptr); //Continue with same value of ptr...
                  p.par.FillPitParas(p.pr, lgMean);
                  b.px = p.px = px;
                  p.sidex = (side)ab;
               }
            
            }
            //f.Close();

      }

      /// <summary>
      /// Breaks down string, stats, using GetHex(), filling br
      /// </summary>
      /// ----------------------------------------------------------------
      private void FillBatStats(string stats, ref CBatRealSet br, ref int ptr) {
     
      // GetHex returns -1 if db_stats is all F's -- this indicates missing.
  
         br.ab = CBCXCommon.GetHex(stats, ref ptr, 3);
         if (br.ab > 15) {
            br.hr = CBCXCommon.GetHex(stats, ref ptr, 2);
            br.bi = CBCXCommon.GetHex(stats, ref ptr, 2);
            br.sb = CBCXCommon.GetHex(stats, ref ptr, 2);
            br.cs = CBCXCommon.GetHex(stats, ref ptr, 2);
            br.h = CBCXCommon.GetHex(stats, ref ptr, 3);
            br.ave = Math.Round((double)br.h / (double)br.ab,3);
            br.b2 = CBCXCommon.GetHex(stats, ref ptr, 2);
            br.b3 = CBCXCommon.GetHex(stats, ref ptr, 2);
            br.bb = CBCXCommon.GetHex(stats, ref ptr, 2);
            br.so = CBCXCommon.GetHex(stats, ref ptr, 3);
         }
         else {
         // 15 or fewer ab's: all stats 1 digit...
            br.hr = CBCXCommon.GetHex(stats, ref ptr, 1);
            br.bi = CBCXCommon.GetHex(stats, ref ptr, 1);
            br.sb = CBCXCommon.GetHex(stats, ref ptr, 1);
            br.cs = CBCXCommon.GetHex(stats, ref ptr, 1);
            br.h = CBCXCommon.GetHex(stats, ref ptr, 1);
            br.ave = 0.0;
            br.b2 = CBCXCommon.GetHex(stats, ref ptr, 1);
            br.b3 = CBCXCommon.GetHex(stats, ref ptr, 1);
            br.bb = CBCXCommon.GetHex(stats, ref ptr, 1);
            br.so = CBCXCommon.GetHex(stats, ref ptr, 1);
         }

      }


      private void FillPitStats(string stats, ref CPitRealSet pr, ref int ptr) {
      // -------------------------------------------------------------------
      // GetHex returns -1 if db_stats is all F's -- this indicates missing.

         pr.g = CBCXCommon.GetHex(stats, ref ptr, 2);
         pr.gs = CBCXCommon.GetHex(stats, ref ptr, 2);
         pr.w = CBCXCommon.GetHex(stats, ref ptr, 2);
         pr.l = CBCXCommon.GetHex(stats, ref ptr, 2);
         pr.bfp = CBCXCommon.GetHex(stats, ref ptr, 3);
         pr.ip3 = CBCXCommon.GetHex(stats, ref ptr, 3);
         pr.h = CBCXCommon.GetHex(stats, ref ptr, 3);
         pr.er = CBCXCommon.GetHex(stats, ref ptr, 3);
         pr.hr = CBCXCommon.GetHex(stats, ref ptr, 3);
         pr.so = CBCXCommon.GetHex(stats, ref ptr, 3);
         pr.bb = CBCXCommon.GetHex(stats, ref ptr, 3);
         pr.sv = CBCXCommon.GetHex(stats, ref ptr, 2);
         pr.era = pr.ip3 == 0.0 ? 0.0 : pr.er / ((double)pr.ip3 / 3.0) * 9.0;
      }



   }
}
