﻿/*
TTMPlayer.cs

This file is part of Johnny Castaway Open Source.

Copyright (c) 2015 Hans Milling

Johnny Castaway Open Source is free software: you can redistribute it and/or modify it under the terms of the
GNU General Public License as published by the Free Software Foundation, either version 3 of the License,
or (at your option) any later version.
Johnny Castaway Open Source is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU General Public License for more details. You should have received a copy of the
GNU General Public License along with Johnny Castaway Open Source. If not, see http://www.gnu.org/licenses/.

*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.ComponentModel;
using System.Media;
using System.IO;
using System.Diagnostics;

namespace SCRANTIC
{
  public class TTMPlayer
  {
    private const int MAX_IMAGE_SLOTS   = 10;
    private const int MAX_PALETTE_SLOTS = 10;

    private UInt16 currFrame;
    private UInt16 currImage;
    //private UInt16 currPalette;
    //private UInt16 currChunk;
    private UInt16 currDelay;
    //private UInt16 currSound;
    //private bool paletteActivated;
    private Resource.BMP[] imageSlot = new Resource.BMP[MAX_IMAGE_SLOTS];

    private Graphics g;
    //private Dictionary<int,List<Instruction>> scripts;
    private Bitmap backgroundImage = null;
    private bool backgroundImageDrawn = false;
    private Bitmap savedImage = null;
    private bool savedImageDrawn = false;
    private Bitmap screenSlot = null;
    private int xSavedImage = 0;
    private int ySavedImage = 0;

    public bool Playing { get; set; }

    public BackgroundWorker bw = new BackgroundWorker();
    public Bitmap screen = null;

    private SoundPlayer player = new SoundPlayer();

    Resource.TTM ttm = null;

    private bool initialized = false;

    public TTMPlayer(Resource.TTM ttmobj , Bitmap scr)
    {
      ttm = ttmobj;
      screen = scr;
      g = Graphics.FromImage(screen);
      g.DrawRectangle(new Pen(Color.Red), new Rectangle(0, 0, 640, 480));

      //scripts = scriptlist;
      bw.WorkerReportsProgress = true;
      bw.WorkerSupportsCancellation = true;
      bw.DoWork += play;
    }
    /*
    public void setScript(Dictionary<int, List<Instruction>> scriptlist)
    {
      scripts = scriptlist;
    }
    */

    public void playscript(UInt16 no)
    {
      foreach (Instruction mc in ttm.scripts[no])
      {
        //System.Diagnostics.Debug.Write(no.ToString("X2") + "\t" + ttm.tags[no].PadRight(20) + "\t");
        //System.Diagnostics.Debug.WriteLine(mc.ToString());
        if (bw.CancellationPending)
        {
          Playing = false;
          return;
        }
        switch (mc.code)
        {
          case Instruction.SAVE_BACKGROUND:
            {
              if (backgroundImage == null)
              {
                backgroundImage = new Bitmap(640, 480);
              }
              Graphics gr = Graphics.FromImage(backgroundImage);
              gr.DrawImage(screen, 0, 0, new Rectangle(0, 0, 640, 480), GraphicsUnit.Pixel);


              backgroundImageDrawn = false;
              break;
            }
          case Instruction.DRAW_BACKGROUND:
            {
              if (backgroundImage != null)
              {
                g.DrawImageUnscaled(backgroundImage, 0, 0);
                g.Save();
                backgroundImageDrawn = true;
              }
              break;
            }
          case Instruction.PURGE:
            {/*
                  if (backgroundImage != null)
                  {
                    backgroundImage.Dispose();
                    backgroundImage = null;
                  }
                  if (savedImage != null)
                  {
                    savedImage.Dispose();
                    savedImage = null;
                  }*/
              break;
            }
          case Instruction.UPDATE:
            {
              if ((backgroundImage != null) && (!backgroundImageDrawn))
              {
                g.DrawImageUnscaled(backgroundImage, 0, 0);
                g.Save();
                backgroundImageDrawn = true;
              }
              if ((savedImage != null) && (!savedImageDrawn))
              {
                g.DrawImageUnscaled(savedImage, xSavedImage, ySavedImage);
                g.Save();
                savedImageDrawn = true;
              }
              /*if ( !paletteActivated )
              {
                  paletteSlot[currPalette].GetPalette().Activate ( 0, WINDOW_COLORS );
                  paletteActivated = true;
              } */
              //media.GetVideo().Refresh();
              if (currDelay > 0)
              {
                Thread.Sleep(currDelay);
              }
              /* else
                 Thread.Sleep(50);*/
              backgroundImageDrawn = false;
              savedImageDrawn = false;
              bw.ReportProgress(0, new Bitmap(screen));
              break;
            }
          case Instruction.DELAY:
            {
              currDelay = (UInt16)(mc.data[0] * 20);
              break;
            }
          case Instruction.SLOT_IMAGE:
            {
              currImage = mc.data[0];
              break;
            }
          case Instruction.SLOT_PALETTE:
            {
              /*currPalette = mc.data[0];
              paletteActivated = false;*/
              break;
            }
          case Instruction.SET_SCENE:
            {
              break;
            }
          case Instruction.SKIP_NEXT_IF:
            {
              Debug.WriteLine(mc.data[0]+" "+mc.data[1]+" ("+ttm.tags[mc.data[1]]+")");
              break;
            }
          case Instruction.SET_FRAME0:
            {
              currFrame = mc.data[1];
              break;
            }
          case Instruction.SET_FRAME1:
            {
              currFrame = mc.data[1];
              break;
            }
          case Instruction.FADE_OUT:
            {
              g.Clear(Color.Black);
              //paletteActivated = true;
              break;
            }
          case Instruction.FADE_IN:
            {
              //paletteActivated = true;
              break;
            }
          case Instruction.SAVE_IMAGE0:
            {
              xSavedImage = mc.data[0];
              ySavedImage = mc.data[1];
              savedImage = new Bitmap(mc.data[2], mc.data[3]);
              Graphics gr = Graphics.FromImage(savedImage);
              //Bitmap screen = new Bitmap(640, 480, g);
              gr.DrawImage(screen, 0, 0, new Rectangle(xSavedImage, ySavedImage, savedImage.Width, savedImage.Height), GraphicsUnit.Pixel);
              savedImageDrawn = false;
              break;
            }
          case Instruction.SAVE_IMAGE1:
            {
              xSavedImage = mc.data[0];
              ySavedImage = mc.data[1];
              savedImage = new Bitmap(mc.data[2], mc.data[3]);
              Graphics gr = Graphics.FromImage(savedImage);
              //Bitmap screen = new Bitmap(640, 480, g);
              gr.DrawImage(screen, 0, 0, new Rectangle(xSavedImage, ySavedImage, savedImage.Width, savedImage.Height), GraphicsUnit.Pixel);
              savedImageDrawn = false;
              //g.DrawRectangle(new Pen(Color.Blue), xSavedImage, ySavedImage, savedImage.Width, savedImage.Height);
              break;
            }
          case Instruction.DRAW_WHITE_LINE:
            {
              int x1 = mc.data[0];
              int y1 = mc.data[1];
              int x2 = mc.data[2];
              int y2 = mc.data[3];
              g.DrawLine(new Pen(Color.White), x1, y1, x2, y2);
              break;
            }
          case Instruction.SET_WINDOW0:
            {
              int x = mc.data[0];
              int y = mc.data[1];
              int width = mc.data[2];
              int height = mc.data[3];
              g.DrawRectangle(new Pen(Color.Cyan), x, y, width, height);
              g.Save();
              g.Clip = new Region(new RectangleF(x, y, width, height));
              break;
            }
          case Instruction.DRAW_BUBBLE:
            {
              Brush brush = new SolidBrush(Color.White);
              Pen pen = new Pen(Color.Black);
              int x = mc.data[0];
              int y = mc.data[1];
              int width = mc.data[2];
              int height = mc.data[3];
              g.FillEllipse(brush, x, y, width, height);
              g.DrawEllipse(pen, x, y, width, height);
              break;
            }
          case Instruction.SET_WINDOW1:
            {
              int x1 = mc.data[0];
              int y1 = mc.data[1];
              int x2 = mc.data[2];
              int y2 = mc.data[3];
              int width = x2 - x1;
              int height = y2 - y1;
              g.DrawRectangle(new Pen(Color.Cyan), x1, x2, width, height);
              g.Save();
              g.Clip = new Region(new RectangleF(x1, y1, width, height));
              break;
            }
          case Instruction.DRAW_SPRITE0:
            {
              if ((backgroundImage != null) && (!backgroundImageDrawn))
              {
                g.DrawImageUnscaled(backgroundImage, 0, 0);
                backgroundImageDrawn = true;
              }
              if ((savedImage != null) && (!savedImageDrawn))
              {
                g.DrawImageUnscaled(savedImage, xSavedImage, ySavedImage);
                savedImageDrawn = true;
              }
              /*if (imageSlot[mc.data[3]] != null)
              {*/
                g.DrawImageUnscaled(imageSlot[mc.data[3]].images[mc.data[2]], mc.data[0], mc.data[1]); // TODO: draw specific image
              //}
              break;
            }
          case Instruction.DRAW_SPRITE1:
            throw new Exception("Not implemented");
          case Instruction.DRAW_SPRITE2:
            {
              if ((backgroundImage != null) && (!backgroundImageDrawn))
              {
                g.DrawImageUnscaled(backgroundImage, 0, 0);
                backgroundImageDrawn = true;
              }
              if ((savedImage != null) && (!savedImageDrawn))
              {
                g.DrawImageUnscaled(savedImage, xSavedImage, ySavedImage);
                savedImageDrawn = true;
              }
              /*if (imageSlot[mc.data[3]] != null)
              {*/
              int w = imageSlot[mc.data[3]].images[mc.data[2]].Width;
              int h = imageSlot[mc.data[3]].images[mc.data[2]].Height;
              g.DrawImage(imageSlot[mc.data[3]].images[mc.data[2]], mc.data[0] + w, mc.data[1], -w, h); // TODO: draw specific image
              //}
              break;
            }
          case Instruction.DRAW_SPRITE3:
            throw new Exception("Not implemented");
          case Instruction.DRAW_SCREEN:
            {
              if ((backgroundImage != null) && (!backgroundImageDrawn))
              {
                g.DrawImageUnscaled(backgroundImage, 0, 0);
                backgroundImageDrawn = true;
              }
              if (screenSlot != null)
              {
                g.DrawImageUnscaled(screenSlot, mc.data[0], mc.data[1]);
              }
              break;
            }
          case Instruction.LOAD_SOUND:
            {
              break;
            }
          case Instruction.SELECT_SOUND:
            {
              /* if (soundplayering)
                   stop playing sound
                 soundplayer.load(mc.data[0])
               */
              break;
            }
          case Instruction.DESELECT_SOUND:
            {
              /*
                 stop sound player and clear it
               */
              break;
            }
          case Instruction.PLAY_SOUND:
            {
              player = new SoundPlayer();
              Log.write("Playing sound " + mc.data[0]);
              switch (mc.data[0])
              {
                case 1:
                  player.Stream = SCRANTIC.Properties.Resources.sound1; break;
                case 2:
                  player.Stream = SCRANTIC.Properties.Resources.sound2; break;
                case 3:
                  player.Stream = SCRANTIC.Properties.Resources.sound3; break;
                case 4:
                  player.Stream = SCRANTIC.Properties.Resources.sound4; break;
                case 5:
                  player.Stream = SCRANTIC.Properties.Resources.sound5; break;
                case 6:
                  player.Stream = SCRANTIC.Properties.Resources.sound6; break;
                case 7:
                  player.Stream = SCRANTIC.Properties.Resources.sound7; break;
                case 8:
                  player.Stream = SCRANTIC.Properties.Resources.sound8; break;
                case 9:
                  player.Stream = SCRANTIC.Properties.Resources.sound9; break;
                case 10:
                  player.Stream = SCRANTIC.Properties.Resources.sound10; break;
                case 11:
                  player.Stream = SCRANTIC.Properties.Resources.sound11; break;
                case 12:
                  player.Stream = SCRANTIC.Properties.Resources.sound12; break;
                case 13:
                  player.Stream = SCRANTIC.Properties.Resources.sound13; break;
                case 14:
                  player.Stream = SCRANTIC.Properties.Resources.sound14; break;
                case 15:
                  player.Stream = SCRANTIC.Properties.Resources.sound15; break;
                case 16:
                  player.Stream = SCRANTIC.Properties.Resources.sound16; break;
                case 17:
                  player.Stream = SCRANTIC.Properties.Resources.sound17; break;
                case 18:
                  player.Stream = SCRANTIC.Properties.Resources.sound18; break;
                case 19:
                  player.Stream = SCRANTIC.Properties.Resources.sound19; break;
                case 20:
                  player.Stream = SCRANTIC.Properties.Resources.sound20; break;
                case 21:
                  player.Stream = SCRANTIC.Properties.Resources.sound21; break;
                case 22:
                  player.Stream = SCRANTIC.Properties.Resources.sound22; break;
                case 23:
                  player.Stream = SCRANTIC.Properties.Resources.sound23; break;
                default:
                  System.Diagnostics.Debug.WriteLine("Unknown sound number: " + mc.data[0].ToString()); break;
              }

              //                  string soundfile = @"C:\source\Johnny Castaway\Tools\BmpConvert\BmpConvert\Debug\Src\sound" + mc.data[0].ToString() + ".wav";
              //                if (File.Exists(soundfile))
              //              {
              //              SoundPlayer simpleSound = new SoundPlayer(soundfile);
              player.Play();
              //          }
              // play sound mc.data[0]
              break;
            }
          case Instruction.STOP_SOUND:
            {
              // player.stop(mc.data[0]);
              break;
            }
          case Instruction.LOAD_SCREEN:
            {
              /* Need to find a way to actually dispose the bitmaps, as they cause memory leaks.
               * if ( screenSlot != null)
              {
                  screenSlot.Dispose();
              }*/
              //mc.name = mc.name.Substring(0, mc.name.Length-1)+"X";
              screenSlot = ((Resource.SCR)ResourceManager.get(mc.name)).image;
              g.DrawImageUnscaled(screenSlot, 0, 0);
              g.FillRectangle(new SolidBrush(Color.Blue), new Rectangle(0, 350, 640, 130));
              break;
            }
          case Instruction.LOAD_IMAGE:
            {
              if (imageSlot[currImage] != null)
              {
                //imageSlot[currImage].Dispose();
                imageSlot[currImage] = null;
              }
              //mc.name = mc.name.Substring(0, mc.name.Length - 1) + "X";
              imageSlot[currImage] = ResourceManager.get(mc.name) as Resource.BMP;
              break;
            }
          case Instruction.LOAD_PALETTE:
            {
              /*if ( paletteSlot[currPalette] )
              {
                  delete paletteSlot[currPalette];
              }
              paletteSlot[currPalette] = new PaletteResource;
              FileManager::GetInstance().Load ( paletteSlot[currPalette], mc.name );
              paletteActivated = false;*/
              break;
            }
          case Instruction.UNKNOWN2:
            break;
          default:
            {
              break;
            }
        }
        /*currChunk++;
        if ( currChunk == chunkVec.size() )
        {
            if ( looped )
            {
                currChunk = 0;
                if ( backgroundImage != 0 )
                {
                    delete backgroundImage;
                    backgroundImage = 0;
                }
                if ( savedImage != 0 )
                {
                    delete savedImage;
                    savedImage = 0;
                }
            }
            else
            {
                playing = false;
            }
        }*/
      }
    }

    public void play(object sender, DoWorkEventArgs e)
    {
      Playing = true;
      UInt16 scriptno = (UInt16)e.Argument;
      if (!initialized)
      {
        initialized = true;
        if (ttm.NeedsInit)
          playscript(0);
      }
      playscript(scriptno);
      Playing = false;
    }

    /*
    private void animateImageOpacity(PictureBox control)
    {
        for(float i = 0F; i< 1F; i+=.10F)
        {
            control.Image = ChangeOpacity(itemIcon[selected], i);
            Thread.Sleep(40);
        }
    }

    public static Bitmap ChangeOpacity(Image img, float opacityvalue)
    {
        Bitmap bmp = new Bitmap(img.Width, img.Height); // Determining Width and Height of Source Image
        Graphics graphics = Graphics.FromImage(bmp);
        ColorMatrix colormatrix = new ColorMatrix {Matrix33 = opacityvalue};
        ImageAttributes imgAttribute = new ImageAttributes();
        imgAttribute.SetColorMatrix(colormatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        graphics.DrawImage(img, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imgAttribute);
        graphics.Dispose();   // Releasing all resource used by graphics 
        return bmp;
    }
     * */

  }
}
