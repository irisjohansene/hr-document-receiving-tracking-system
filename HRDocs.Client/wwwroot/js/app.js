window.signaturePad = {
  init: id => { const c=document.getElementById(id); if(!c)return; const x=c.getContext('2d'); x.lineWidth=2;x.lineCap='round';x.strokeStyle='#17324d'; let down=false; const p=e=>{const r=c.getBoundingClientRect(),s=e.touches?e.touches[0]:e;return{x:(s.clientX-r.left)*c.width/r.width,y:(s.clientY-r.top)*c.height/r.height}}; const start=e=>{down=true;const q=p(e);x.beginPath();x.moveTo(q.x,q.y);e.preventDefault()};const move=e=>{if(!down)return;const q=p(e);x.lineTo(q.x,q.y);x.stroke();e.preventDefault()};c.onpointerdown=start;c.onpointermove=move;c.onpointerup=c.onpointerleave=()=>down=false; },
  clear: id => {const c=document.getElementById(id);if(c)c.getContext('2d').clearRect(0,0,c.width,c.height)},
  data: id => document.getElementById(id)?.toDataURL('image/png')||''
};
window.setApiBase = value => window.hrdocsApiBase = value;
window.authDownload = async (url,token,name) => { const base=document.querySelector('base').href; const api=(window.hrdocsApiBase||base); const r=await fetch(new URL(url,api),{headers:{Authorization:`Bearer ${token}`}});if(!r.ok)throw new Error(await r.text());const b=await r.blob(),a=document.createElement('a');a.href=URL.createObjectURL(b);a.download=name;a.click();URL.revokeObjectURL(a.href); };
