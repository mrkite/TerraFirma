/*
Copyright (c) 2012, Sean Kasun
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE.
*/

#include <stdio.h>
#include <dirent.h>
#include <sys/stat.h>
#include <stdint.h>
#include <string.h>
#include <stdlib.h>
#include <png.h>
#include "lzx.h"

static inline uint16_t r16(uint8_t *p)
{
	uint16_t r=p[0]|(p[1]<<8);
	return r;
}

static inline uint32_t r32(uint8_t *p)
{
	uint32_t r=p[0]|(p[1]<<8)|(p[2]<<16)|(p[3]<<24);
	return r;
}

static void outputPng(FILE *f,uint8_t *data,uint32_t width,uint32_t height)
{
	png_bytep *row_pointers;
	png_structp png_ptr;
	png_infop info_ptr;
	int i;
	row_pointers=(png_bytep*)malloc(sizeof(png_bytep)*height);
	for (i=0;i<height;i++)
		row_pointers[i]=data+(width*4*i);
	png_ptr=png_create_write_struct(PNG_LIBPNG_VER_STRING,NULL,NULL,NULL);
	if (!png_ptr)
	{
		free(row_pointers);
		return;
	}
	info_ptr=png_create_info_struct(png_ptr);
	if (!info_ptr)
	{
		png_destroy_write_struct(&png_ptr,(png_infopp)NULL);
		free(row_pointers);
		return;
	}
	if (setjmp(png_jmpbuf(png_ptr)))
	{
		png_destroy_write_struct(&png_ptr,&info_ptr);
		free(row_pointers);
		return;
	}
	png_init_io(png_ptr,f);
	png_set_IHDR(png_ptr,info_ptr,width,height,8,
		PNG_COLOR_TYPE_RGB_ALPHA,PNG_INTERLACE_NONE,
		PNG_COMPRESSION_TYPE_DEFAULT,PNG_FILTER_TYPE_DEFAULT);
	png_write_info(png_ptr,info_ptr);
	png_write_image(png_ptr,row_pointers);
	png_write_end(png_ptr,NULL);
	png_destroy_write_struct(&png_ptr,&info_ptr);
	free(row_pointers);
}

static void convert(char *path,char *file,char *dest)
{
	int inlen=strlen(path)+1+strlen(file)+1;
	char *inname=malloc(inlen);
	snprintf(inname,inlen,"%s/%s",path,file);
	FILE *f=fopen(inname,"rb");
	fseek(f,0,SEEK_END);
	int len=ftell(f);
	fseek(f,0,SEEK_SET);
	uint8_t *raw=malloc(len);
	fread(raw,1,len,f);
	fclose(f);
	free(inname);

	uint8_t *p=raw;

	//verify XNB
	uint32_t header=r32(p); p+=4;
	//header is XNBw XNBx XNBm, for windows, unix, mac
	if (header!=0x77424e58 && header!=0x78424e58 && header!=0x6d424e58)
	{
		fprintf(stderr,"%s: Not a valid XNB file\n",file);
		free(raw);
		return;
	}
	uint16_t version=r16(p); p+=2;
	int compressed=version&0x8000;
	version&=0xff; //ignore graphics profile
	if (version!=5 && version!=4)
	{
		fprintf(stderr,"%s: Invalid XNB Version\n",file);
		free(raw);
		return;
	}
	uint32_t length=r32(p); p+=4; //length of entire file
	uint8_t *endp=raw+length;
	if (compressed)
	{
		uint32_t decompLength=r32(p); p+=4;
		uint8_t *decomp=malloc(decompLength);
		uint8_t *dp=decomp;
		struct LZXstate *lzx=LZXinit(16);
		while (p<endp)
		{
			uint8_t hi=*p++;
			uint8_t lo=*p++;
			uint16_t compLen=(hi<<8)|lo;
			uint16_t decompLen=0x8000;
			if (hi==0xff)
			{
				hi=lo;
				lo=*p++;
				decompLen=(hi<<8)|lo;
				hi=*p++;
				lo=*p++;
				compLen=(hi<<8)|lo;
			}
			if (compLen==0 || decompLen==0) //done
				break;
			LZXdecompress(lzx,p,dp,compLen,decompLen);
			p+=compLen;
			dp+=decompLen;
		}
		LZXteardown(lzx);
		free(raw);
		raw=decomp;
		p=raw;
		endp=raw+decompLength;
	}

	// skip readers
	int numReaders=*p++;
	int i;
	for (i=0;i<numReaders;i++)
	{
		int nameLen=*p++;
		nameLen++;
		p+=nameLen;
		p+=4; //skip version
	}
	p++; //padding
	p++; //reader index (should always be 1)
	// at this point we're at the start of the actual image data
	// if we aren't reading a texture xnb (which you can confirm by
	// actually checking the readers above), we're gonna crash and burn
	// right here.
	uint32_t format=r32(p); p+=4;
	uint32_t width=r32(p); p+=4;
	uint32_t height=r32(p); p+=4;
	p+=4; //level count
	uint32_t imageLen=r32(p); p+=4;
	if (format>19)
	{
		fprintf(stderr,"%s: Invalid image format\n",file);
		free(raw);
		return;
	}
	uint8_t *pixels=malloc(width*height*4);

	// convert all formats to RGBA32
	uint32_t outofs,ofs,blocksWide,blocksHigh,x,y;
	uint8_t r,g,b,a;
	switch (format)
	{
		case 0: //Color		(rrrrrrrr gggggggg bbbbbbbb aaaaaaaa)
			for (outofs=0,ofs=0;ofs<width*height*4;ofs+=4,outofs+=4)
			{
				pixels[outofs]=p[ofs]; //r
				pixels[outofs+1]=p[ofs+1]; //g
				pixels[outofs+2]=p[ofs+2]; //b
				pixels[outofs+3]=p[ofs+3]; //a
			}
			break;
		case 1: //Bgr565	(bbbbbggg gggrrrrr)
			for (outofs=0,ofs=0;ofs<width*height*2;ofs+=2,outofs+=4)
			{
				r=p[ofs+1]&0x1f;
				g=(p[ofs+1]>>5)|((p[ofs]&0x7)<<3);
				b=p[ofs]>>3;
				pixels[outofs]=(255*r)/0x1f;
				pixels[outofs+1]=(255*g)/0x3f;
				pixels[outofs+2]=(255*b)/0x1f;
				pixels[outofs+3]=255; //a
			}
			break;
		case 2: //Bgra5551	(bbbbbggg ggrrrrra)
			for (outofs=0,ofs=0;ofs<width*height*2;ofs+=2,outofs+=4)
			{
				r=(p[ofs+1]&0x3e)>>1;
				g=(p[ofs+1]>>6)|((p[ofs]&0x7)<<2);
				b=p[ofs]>>3;
				a=p[ofs+1]&1;
				pixels[outofs]=(255*r)/0x1f;
				pixels[outofs+1]=(255*g)/0x1f;
				pixels[outofs+2]=(255*b)/0x1f;
				pixels[outofs+3]=255*a;
			}
			break;
		case 3: //Bgra4444	(bbbbgggg rrrraaaa)
			for (outofs=0,ofs=0;ofs<width*height*2;ofs+=2,outofs+=4)
			{
				r=p[ofs+1]>>4;
				g=p[ofs]&0xf;
				b=p[ofs]>>4;
				a=p[ofs+1]&0xf;
				pixels[outofs]=(255*r)/0xf;
				pixels[outofs+1]=(255*g)/0xf;
				pixels[outofs+2]=(255*b)/0xf;
				pixels[outofs+3]=(255*a)/0xf;
			}
			break;
		case 4: //Dxt1		(compressed, then Color)
		case 5: //Dxt3		(compressed, then Color)
		case 6: //Dxt5		(compressed, then Color)
		case 7: //NormalizedByte2 (16-bit bump map)
		case 8: //NormalizedByte4 (32-bit bump map)
		case 9: //Rgba1010102	(rrrrrrrr rrgggggg ggggbbbb bbbbbbaa)
		case 10://Rg32		(rrrrrrrr rrrrrrrr gggggggg gggggggg)
		case 11://Rgba64	(rrrrrrrr rrrrrrrr gggggggg gggggggg
			//		 bbbbbbbb bbbbbbbb aaaaaaaa aaaaaaaa)
		case 12://Alpha8	(aaaaaaaa)
		case 13://Single	red channel only, 32-bit floats
		case 14://Vector2	float red, float green
		case 15://Vector4	float alpha, blue, green, red
		case 16://HalfSingle	Same as Single, but 16-bit floats
		case 17://HalfVector2	Same as Vector2, but 16-bit floats
		case 18://HalfVector4	Same as Vector4, but 16-bit floats
		case 19://HdrBlendable	floats
			fprintf(stderr,"%s: Unsupported format\n",file);
			free(pixels);
			free(raw);
			return;
	}

	int outlen=strlen(dest)+1+strlen(file)+1;
	char *outname=malloc(outlen);
	snprintf(outname,outlen,"%s/%s",dest,file);
	memcpy(outname+outlen-4,"png",3); //.xnb -> .png
	printf("%s: %s\n",file,outname);
	f=fopen(outname,"wb");
	outputPng(f,pixels,width,height);
	fclose(f);
	free(outname);
	free(pixels);
	
	free(raw);
}

int main(int argc,char *argv[])
{
	if (argc!=3)
	{
		fprintf(stderr,"Usage: %s <xnb directory> <output directory>\n",argv[0]);
		return -1;
	}

	struct stat stats;
	if (!stat(argv[2],&stats)) //file exists
	{
		if (!S_ISDIR(stats.st_mode)) //not a dir
		{
			fprintf(stderr,"%s is not a directory\n",argv[2]);
			return -1;
		}
	}
	else //doesn't exist, let's make it
		mkdir(argv[2],0777);
	
	DIR *dir=opendir(argv[1]);
	if (!dir)
	{
		fprintf(stderr,"Couldn't open directory %s\n",argv[1]);
		return -1;
	}
	struct dirent *file;
	while ((file=readdir(dir)))
	{
		if (file->d_type==DT_DIR) continue;
		convert(argv[1],file->d_name,argv[2]);
	}
	closedir(dir);
}
