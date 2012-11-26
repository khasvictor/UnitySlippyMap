// 
//  TileDownloader.cs
//  
//  Author:
//       Jonathan Derrough <jonathan.derrough@gmail.com>
//  
//  Copyright (c) 2012 Jonathan Derrough
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

//#define DEBUG_LOG

using UnityEngine;

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;

// <summary>
// A singleton class in charge of downloading, caching and serving tiles.
// </summary>
public class TileDownloader : MonoBehaviour
{
	#region Singleton implementation
	
	private static TileDownloader instance = null;
	public static TileDownloader Instance
	{
		get
		{
            if (null == (object)instance)
            {
                instance = FindObjectOfType(typeof (TileDownloader)) as TileDownloader;
                if (null == (object)instance)
                {
                    var go = new GameObject("[TileDownloader]");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    instance = go.AddComponent<TileDownloader>();
                    instance.EnsureDownloader();
                }
			}

			return instance;
		}
	}

	private void EnsureDownloader()
	{
		LoadTiles();
	}
	
	private TileDownloader()
	{
	}
	
	private void OnApplicationQuit()
	{
		DestroyImmediate(this.gameObject);
	}
	
	#endregion

    private class AsyncInfo
    {
        private TileEntry entry;
        public TileEntry Entry { get { return entry;  } }

        private FileStream fs;
        public FileStream FS { get { return fs; } }

        public AsyncInfo(TileEntry entry, FileStream fs)
        {
            this.entry = entry;
            this.fs = fs;
        }
    }

	// <summary>
	// The TileEntry class holds the information necessary to the TileDownloader to manage the tiles.
	// It also handles the (down)loading/caching of the concerned tile, taking advantage of Prime31's JobManager
	// </summary>
	public class TileEntry
	{
		[XmlAttribute("timestamp")]
		public double	timestamp;
		[XmlAttribute("size")]
		public int		size;
		[XmlAttribute("guid")]
		public string	guid;
		
		[XmlAttribute("url")]
		public string	url;
        [XmlIgnore]
        public Tile     tile;
        [XmlIgnore]
        public Texture2D texture;
        [XmlIgnore]
		public bool		cached = false;
		[XmlIgnore]
		public bool		error = false;
		
		[XmlIgnore]
		public Job		job;
        [XmlIgnore]
        public Job.JobCompleteHandler jobCompleteHandler;

		public TileEntry()
		{
            this.jobCompleteHandler = new Job.JobCompleteHandler(TileDownloader.Instance.JobTerminationEvent);
		}
		
		public TileEntry(string url, Tile tile)
		{
			this.url = url;
            if (tile == null)
                throw new ArgumentNullException("tile");
            this.tile = tile;
            this.jobCompleteHandler = new Job.JobCompleteHandler(TileDownloader.Instance.JobTerminationEvent);
		}
		
		public void StartDownload()
		{
#if DEBUG_LOG
			Debug.Log("DEBUG: TileEntry.StartDownload: " + url);
#endif
			job = new Job(DownloadCoroutine(), this);
			job.JobComplete += jobCompleteHandler;
		}
		
		public void StopDownload()
		{
#if DEBUG_LOG
			Debug.Log("DEBUG: TileEntry.StopDownload: " + url);
#endif
            job.JobComplete -= jobCompleteHandler;
			job.Kill();
		}
		
		private IEnumerator DownloadCoroutine()
		{
			WWW www = null;
            if (cached && File.Exists(Application.persistentDataPath + "/" + this.guid + ".png"))
            {
                www = new WWW("file:///" + Application.persistentDataPath + "/" + this.guid + ".png");
#if DEBUG_LOG
                Debug.Log("DEBUG: TileDownloader.DownloadCoroutine: loading tile from cache: url: " + www.url);
#endif
            }
            else
            {
                www = new WWW(url);
#if DEBUG_LOG
                Debug.Log("DEBUG: TileDownloader.DownloadCoroutine: loading tile from provider: url: " + www.url + "(cached: " + cached + ")");
#endif
            }
				
			yield return www;
			
			if (www.error == null && www.text.Contains("404 Not Found") == false)
			{
                if (www.texture.isBogus())
                {
#if DEBUG_LOG
                    Debug.LogError("DEBUG: TileEntry.DownloadCoroutine: image from cache is bogus, trying to download it: " + www.url + " [" + url + "]");
#endif
                    //TileDownloader.Instance.DeleteCachedTile(this);
                    //TileDownloader.Instance.Get(url, material);
                    error = true;
                }
                else
                {
                    Texture2D texture = www.texture;
					
					/*
					tile.TextureId = TextureAtlasManager.Instance.AddTexture(texture);
                    TextureAtlas.TextureInfo textureInfo = TextureAtlasManager.Instance.GetTextureInfo(tile.TextureId);
                    Material sharedMaterial = SharedMaterialManager.Instance.GetSharedMaterial(textureInfo.Texture.name, "Somian/Unlit/Transparent");
                    GameObject gameObject = tile.gameObject;
                    gameObject.renderer.sharedMaterial = sharedMaterial;
                    if (sharedMaterial.mainTexture == null)
                    {
                        sharedMaterial.mainTexture = textureInfo.Texture;
                        sharedMaterial.mainTexture.wrapMode = TextureWrapMode.Clamp;
                        sharedMaterial.mainTexture.filterMode = FilterMode.Trilinear;
                    }

                    gameObject.GetComponent<MeshFilter>().mesh.uv = new Vector2[4] {
                        new Vector2(textureInfo.Rect.xMax / textureInfo.Texture.width, textureInfo.Rect.yMax / textureInfo.Texture.height),
                        new Vector2(textureInfo.Rect.xMax / textureInfo.Texture.width, textureInfo.Rect.yMin / textureInfo.Texture.height),
                        new Vector2(textureInfo.Rect.xMin / textureInfo.Texture.width, textureInfo.Rect.yMin / textureInfo.Texture.height),
                        new Vector2(textureInfo.Rect.xMin / textureInfo.Texture.width, textureInfo.Rect.yMax / textureInfo.Texture.height)
                    };
                    */
                    
					Material material = tile.gameObject.renderer.material;
                    material.mainTexture = texture;
                    material.mainTexture.wrapMode = TextureWrapMode.Clamp;
                    material.mainTexture.filterMode = FilterMode.Trilinear;
					tile.renderer.enabled = true;
					tile.Show();
					
                    if (this.cached == false)
    				{
    					// write the png asynchroneously
                        byte[] bytes = texture.EncodeToPNG();
    					
    					this.size = bytes.Length;
    					this.timestamp = (DateTime.Now - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
    					this.guid = Guid.NewGuid().ToString();
    					
    					FileStream fs = new FileStream(Application.persistentDataPath + "/" + this.guid + ".png", FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
    					fs.BeginWrite(bytes, 0, bytes.Length, new AsyncCallback(EndWriteCallback), new AsyncInfo(this, fs));
    				
#if DEBUG_LOG
    					Debug.Log("DEBUG: TileEntry.DownloadCoroutine: done loading: " + www.url + ", writing to cache: " + fs.Name);
#endif
    				}
    				else
    				{
#if DEBUG_LOG
    	    			Debug.Log("DEBUG: TileEntry.DownloadCoroutine: done loading from cache: " + www.url + " [" + url + "]");
#endif
    				}
                }
			}
			else
			{
				error = true;
#if DEBUG_LOG
				Debug.LogError("ERROR: TileEntry.DownloadCoroutine: done downloading: " + www.url + " with error: " + www.error);
#endif
			}
		}
		
		private static void EndWriteCallback(IAsyncResult result)
		{
			AsyncInfo info = result.AsyncState as AsyncInfo;
			info.Entry.cached = true;

            info.FS.EndWrite(result);
            info.FS.Flush();

            info.FS.Close();

#if DEBUG_LOG
			Debug.Log("DEBUG: TileEntry.EndWriteCallback: done writing: " + info.Entry.url + " [" + info.Entry.guid + "]");
#endif
		}
	}
	
	// <summary>
	// Match predicate to find tiles by URL
	// </summary>
	private static string tileURLLookedFor;
	private static bool tileURLMatchPredicate(TileEntry entry)
	{
		if (entry.url == tileURLLookedFor)
			return true;
		return false;
	}
	
	private List<TileEntry>	tileToLoad = new List<TileEntry>();
	private List<TileEntry>	tileLoading = new List<TileEntry>();
	private List<TileEntry>	tiles = new List<TileEntry>();

    private string          tilePath = Application.persistentDataPath;
	
	public int				MaxSimultaneousDownloads = 2;
	public int				MaxCacheSize = 20000000; // 20 Mo
	private int				cacheSize = 0;
		
	#region Public methods
	
	// <summary>
	// Gets a tile by its URL, the main texture of the material is assigned if successful.
	// </summary>
	public void Get(string url, Tile tile)
	{
#if DEBUG_LOG
        Debug.Log("DEBUG: TileDownloader.Get: url: " + url);
#endif
        
		tileURLLookedFor = url;
		if (tileToLoad.Exists(tileURLMatchPredicate))
		{
#if DEBUG_LOG
			Debug.LogWarning("WARNING: TileDownloader.Get: already asked for url: " + url);
#endif
			return ;
		}
		
		if (tileLoading.Exists(tileURLMatchPredicate))
		{
#if DEBUG_LOG
			Debug.LogWarning("WARNING: TileDownloader.Get: already downloading url: " + url);
#endif
			return ;
		}
		
		TileEntry cachedEntry = tiles.Find(tileURLMatchPredicate);

		if (cachedEntry == null)
        {
#if DEBUG_LOG
            Debug.Log("DEBUG: TileDownloader.Get: adding '" + url + "' to loading list");
#endif
            tileToLoad.Add(new TileEntry(url, tile));
        }
		else
		{
#if DEBUG_LOG
            Debug.Log("DEBUG: TileDownloader.Get: adding '" + url + "' to loading list (cached)");
#endif
			cachedEntry.cached = true;
            cachedEntry.tile = tile;
			//cachedEntry.Complete = material;
			tileToLoad.Add(cachedEntry);
		}
	}
	
	// <summary>
	// Cancels the request for a tile by its URL.
	// </summary>
	public void Cancel(string url)
	{
		tileURLLookedFor = url;
		TileEntry entry = tileToLoad.Find(tileURLMatchPredicate);
		if (entry != null)
		{
#if DEBUG_LOG
			Debug.Log("DEBUG: TileDownloader.Cancel: remove download from schedule: " + url);
#endif
			tileToLoad.Remove(entry);
			return ;
		}
		
		entry = tileLoading.Find(tileURLMatchPredicate);
		if (entry != null)
		{
#if DEBUG_LOG
			Debug.Log("DEBUG: TileDownloader.Cancel: stop downloading: " + url);
#endif
            tileLoading.Remove(entry);
			entry.StopDownload();
			return ;
		}

#if DEBUG_LOG
		Debug.LogWarning("WARNING: TileDownloader.Cancel: url not scheduled to be downloaded nor downloading: " + url);
#endif
	}
	
	// <summary>
	// A method called when the job is done, successfully or not.
	// </summary>
	public void JobTerminationEvent(object job, JobEventArgs e)
	{
#if DEBUG_LOG
		Debug.Log("DEBUG: TileDownloader.JobTerminationEvent: Tile download complete, but was it murdered? " + e.WasKilled);
#endif
		TileEntry entry = e.Owner as TileEntry;
		tileLoading.Remove(entry);
		
		if (e.WasKilled == false)
		{
			if (entry.error && entry.cached)
			{
                if (entry.cached)
                {
#if DEBUG_LOG
				    Debug.Log("DEBUG: TileDownloader.JobTerminationEvent: loading cached tile failed, trying to download it: " + entry.url);
#endif
    				// try downloading the tile again
    				entry.cached = false;
					cacheSize -= entry.size;
    				tiles.Remove(entry);
                }
                else
                {
#if DEBUG_LOG
                     Debug.Log("DEBUG: TileDownloader.JobTerminationEvent: downloading tile failed, trying to download it again: " + entry.url);
#endif
                }

				Get(entry.url, entry.tile);
				
				return ;
			}
			
			tiles.Add(entry);
			cacheSize += entry.size;
			
			// if the cache is full, erase the oldest entry
			// FIXME: find a better way to handle the cache (cf. iPhone Maps app)
			// FIXME: one aspect might be to erase tiles in batch, 10 or 20 at a time, a significant number anyway
			if (cacheSize > MaxCacheSize)
			{
                // beware the year 3000 bug :)
				double oldestTimestamp = (new DateTime(3000, 1, 1) - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
				TileEntry entryToErase = null;
				foreach (TileEntry tile in tiles)
				{
					if (tile.timestamp < oldestTimestamp)
					{
						oldestTimestamp = tile.timestamp;
						entryToErase = tile;
					}
				}
				if (entryToErase == null)
				{
#if DEBUG_LOG
					Debug.LogWarning("WARNING: TileDownloader.JobTerminationEvent: no cache entry to erase (should not happen)");
#endif
					return ;
				}

                DeleteCachedTile(entryToErase);
#if DEBUG_LOG
				Debug.Log("DEBUG: TileDownloader.JobTerminationEvent: erased from cache: " + entryToErase.url + " [" + entryToErase.guid + "]");
#endif
			}
		}
	}

	public void PauseAll()
	{
        foreach (TileEntry entry in tileLoading)
        {
            entry.job.Pause();
        }
	}

	public void UnpauseAll()
	{
        foreach (TileEntry entry in tileLoading)
        {
            entry.job.Unpause();
        }
	}
	
	#endregion
	
	#region Private methods
 
    private void Start()
    {
        TextureBogusExtensions.Init(this);
    }
    
	private void Update()
	{
		while (tileToLoad.Count > 0
			&& tileLoading.Count < MaxSimultaneousDownloads)
		{
			DownloadNextTile();
		}
        
#if DEBUG_LOG
        if (tileLoading.Count >= MaxSimultaneousDownloads)
        {
            Debug.Log("DEBUG: TileDownload.Update: tileLoading.Count (" + tileLoading.Count + ") > MaxSimultaneousDownloads (" + MaxSimultaneousDownloads + ")");
            string dbg = "DEBUG: tileLoading entries:\n";
            foreach (TileEntry entry in tileLoading)
            {
                dbg += entry.url + "\n";
            }
            Debug.Log(dbg);
        }
  
        /*
        {
            string dbg = "DEBUG: tileToLoad entries:\n";
            foreach (TileEntry entry in tileToLoad)
            {
                dbg += entry.url + "\n";
            }
            Debug.Log(dbg);
        }
        */
#endif
	}
	
	private void DownloadNextTile()
	{
		TileEntry entry = tileToLoad[0];
		tileToLoad.RemoveAt(0);
		tileLoading.Add(entry);
		
#if DEBUG_LOG
        Debug.Log("DEBUG: TileDownloader.DownloadNextTile: entry.url: " + entry.url);
#endif
        
		entry.StartDownload();		
	}
	   
	private void OnDestroy()
	{
        KillAll();		
		SaveTiles();
		instance = null;
	}
    
    private void KillAll()
    {
        foreach (TileEntry entry in tileLoading)
        {
            entry.job.Kill();
        }
    }
    
    private void DeleteCachedTile(TileEntry t)
    {
        cacheSize -= t.size;
        File.Delete(tilePath + "/" + t.guid + ".png");
        tiles.Remove(t);
    }

	// <summary>
    // Saves the tile informations to an XML file stored in tilePath.
	// </summary>
	private void SaveTiles()
	{
        string filepath = tilePath + "/" + "tile_downloader.xml";
		
#if DEBUG_LOG
		Debug.Log("DEBUG: TileDownloader.SaveTiles: file: " + filepath);
#endif

		XmlSerializer xs = new XmlSerializer(tiles.GetType());
		using (StreamWriter sw = new StreamWriter(filepath))
    	{
			xs.Serialize(sw, tiles);
		}
	}
	
	// <summary>
    // Loads the tile informations from an XML file stored in tilePath.
	// </summary>
	private void LoadTiles()
	{
        string filepath = tilePath + "/" + "tile_downloader.xml";
		
		if (File.Exists(filepath) == false)
		{
#if DEBUG_LOG
			Debug.Log("DEBUG: TileDownloader.LoadTiles: file doesn't exist: " + filepath);
#endif
			return ;
		}
		
#if DEBUG_LOG
		Debug.Log("DEBUG: TileDownloader.LoadTiles: file: " + filepath);
#endif
		
		XmlSerializer xs = new XmlSerializer(tiles.GetType());
		using (StreamReader sr = new StreamReader(filepath))
    	{
			tiles = xs.Deserialize(sr) as List<TileEntry>;
		}
		
		foreach (TileEntry tile in tiles)
		{
			cacheSize += tile.size;
		}
	}
	
	#endregion
}


