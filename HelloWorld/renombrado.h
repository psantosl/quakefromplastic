aaaa
/*****************************************************************************
 * name:		be_aas_bsp.h
 *
 * desc:		AAS
 *
 * $Archive: /source/code/botlib/be_aas_bsp.h $
 *
 *****************************************************************************/

#ifdef AASINTERN
//loads the given BSP file
int AAS_LoadBSPFile(void);
//dump the loaded BSP data
void AAS_DumpBSPData(void);
//unlink the given entity from the bsp tree leaves
void AAS_UnlinkFromBSPLeaves(bsp_link_t *leaves);
//link the given entity to the bsp tree leaves of the given model
bsp_link_t *AAS_BSPLinkEntity(vec3_t absmins,
										vec3_t absmaxs,
										int entnum,
										int modelnum);

//calculates collision with given entity
qboolean AAS_EntityCollision(int entnum,
										vec3_t start,
										vec3_t boxmins,
										vec3_t boxmaxs,
										vec3_t end,
										int contentmask,
										bsp_trace_t *trace);
//for debugging - CAMBIO EN LA MISMA Línea en la segunda rama
void AAS_PrintFreeBSPLinks(char *str);
//
#endif //AASINTERN

#define MAX_EPAIRKEY		128

//trace through the world
//returns the contents at the given point
int AAS_PointContents(vec3_t point);
//returns true when p2 is in the PVS of p1
qboolean AAS_inPVS(vec3_t p1, vec3_t p2);
//returns true when p2 is in the PHS of p1
qboolean AAS_inPHS(vec3_t p1, vec3_t p2);
//returns true if the given areas are connected
qboolean AAS_AreasConnected(int area1, int area2);
//creates a list with entities totally or partly within the given box

bsp_trace_t AAS_Trace(	vec3_t start,
								vec3_t mins,
								vec3_t maxs,
								vec3_t end,
								int passent,
								int contentmask);

